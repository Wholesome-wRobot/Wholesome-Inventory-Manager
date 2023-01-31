using System.Collections.Generic;
using System.Linq;
using Wholesome_Inventory_Manager.Managers.Bags;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Filter;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal class EquipManager : IEquipManager
    {
        private readonly ISkillsManager _skillsManager;
        private readonly ICharacterSheetManager _characterSheetManager;
        private readonly IWIMContainers _containers;
        private readonly ILootFilter _lootFilter;
        private readonly Dictionary<string, bool> _wantedRanged = new Dictionary<string, bool>();
        private SynchronizedCollection<string> _itemEquipAttempts = new SynchronizedCollection<string>();
        private readonly int _maxNbEquipAttempts = 5;
        private readonly object _equipManagerLock = new object();
        private int nbWeaponCombnations = 0; // triggers a message in the log when new combos

        public EquipManager(
            ISkillsManager skillsManager,
            ICharacterSheetManager characterSheetManager,
            IWIMContainers containers,
            ILootFilter lootFilter)
        {
            _skillsManager = skillsManager;
            _characterSheetManager = characterSheetManager;
            _containers = containers;
            _lootFilter = lootFilter;
            _wantedRanged.Add("Bows", AutoEquipSettings.CurrentSettings.EquipBows);
            _wantedRanged.Add("Crossbows", AutoEquipSettings.CurrentSettings.EquipCrossbows);
            _wantedRanged.Add("Guns", AutoEquipSettings.CurrentSettings.EquipGuns);
            _wantedRanged.Add("Thrown", AutoEquipSettings.CurrentSettings.EquipThrown);
        }

        public void Initialize()
        {
            CheckAll();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
        }

        private void OnEventsLuaWithArgs(string id, List<string> args)
        {
            if (id == "BAG_UPDATE"
                || id == "PLAYER_REGEN_ENABLED"
                || id == "UNIT_INVENTORY_CHANGED" && args[0] == "player")
            {
                CheckAll();
            }
        }

        public void CheckAll()
        {
            if (!ObjectManager.Me.IsAlive)
                return;

            lock (_equipManagerLock)
            {
                _characterSheetManager.Scan();
                _containers.Scan();

                if (!ObjectManager.Me.InCombatFlagOnly)
                {
                    _containers.BagEquip();
                    AutoEquipArmor();
                    AutoEquipRings();
                    AutoEquipTrinkets();
                    AutoEquipWeapons();
                    AutoEquipRanged();
                    CheckSwapWeapons();
                }
                AutoEquipAmmo();
                _lootFilter.FilterLoot(_containers.GetAllBagItems());
            }
        }

        private void AutoEquipArmor()
        {
            foreach (ISheetSlot armorSlot in _characterSheetManager.ArmorSlots)
            {
                List<IWIMItem> potentialArmors = GetEquipableFromBags(armorSlot.InvTypes);
                foreach (IWIMItem armorItem in potentialArmors)
                {
                    (ISheetSlot, string) slotToEquip = IsArmorBetter(armorSlot, armorItem);
                    if (slotToEquip != (null, null))
                    {
                        if (EquipItem(slotToEquip.Item1, armorItem, slotToEquip.Item2))
                        {
                            // TODO, update bags and sheet?
                        }
                    }
                }
            }
        }

        // returns the slot in which the item should be equipped, null if the item is not better
        public (ISheetSlot, string) IsArmorBetter(ISheetSlot armorSlot, IWIMItem armorItem, bool isRoll = false)
        {
            if (!CanEquipItem(armorItem, isRoll))
                return (null, null);

            if (armorSlot.Item == null || armorSlot.Item.WeightScore < armorItem.WeightScore)
            {
                string reason = armorSlot.Item == null ?
                    "Nothing equipped in this slot" : $"Replacing {armorSlot.Item.Name} ({armorSlot.Item.WeightScore})";
                return (armorSlot, reason);
            }
            return (null, null);
        }

        private void AutoEquipRings()
        {
            List<IWIMItem> potentialRings = GetEquipableFromBags("INVTYPE_FINGER");
            foreach (IWIMItem ringItem in potentialRings)
            {
                (ISheetSlot, string) slotAndReason = IsRingBetter(ringItem);
                if (slotAndReason != (null, null))
                {
                    if (EquipItem(slotAndReason.Item1, ringItem, slotAndReason.Item2))
                    {
                        // TODO, update bags and sheet?
                    }
                }
            }
        }

        // returns the slot in which the item should be equipped, null if the item is not better
        public (ISheetSlot, string) IsRingBetter(IWIMItem ringItem, bool isRoll = false)
        {

            if (!CanEquipItem(ringItem, isRoll))
            {
                return (null, null);
            }

            ISheetSlot fingerSlot1 = _characterSheetManager.FingerSlots[0];
            ISheetSlot fingerSlot2 = _characterSheetManager.FingerSlots[1];

            if (ringItem.UniqueEquipped)
            {
                if (fingerSlot1.GetItemLink == ringItem.ItemLink
                    || fingerSlot2.GetItemLink == ringItem.ItemLink)
                {
                    return (null, null);
                }
            }

            float ring1Score = fingerSlot1.Item != null ? fingerSlot1.Item.WeightScore : 0;
            float ring2Score = fingerSlot2.Item != null ? fingerSlot2.Item.WeightScore : 0;
            ISheetSlot lowestScoreFingerSlot = ring1Score <= ring2Score ? fingerSlot1 : fingerSlot2;

            if (lowestScoreFingerSlot.Item == null || lowestScoreFingerSlot.Item.WeightScore < ringItem.WeightScore)
            {
                string reason = lowestScoreFingerSlot.Item == null ?
                    "Nothing equipped in this slot" : $"Replacing {lowestScoreFingerSlot.Item.Name} ({lowestScoreFingerSlot.Item.WeightScore})";
                return (lowestScoreFingerSlot, reason);
            }

            return (null, null);
        }

        private void AutoEquipTrinkets()
        {
            List<IWIMItem> potentialTrinkets = GetEquipableFromBags("INVTYPE_TRINKET");
            foreach (IWIMItem trinketItem in potentialTrinkets)
            {
                (ISheetSlot, string) slotAndReason = IsTrinketBetter(trinketItem);
                if (slotAndReason != (null, null))
                {
                    if (EquipItem(slotAndReason.Item1, trinketItem, slotAndReason.Item2))
                    {
                        // TODO, update bags and sheet?
                    }
                }
            }
        }

        // returns the slot in which the item should be equipped, null if the item is not better
        public (ISheetSlot, string) IsTrinketBetter(IWIMItem trinketItem, bool isRoll = false)
        {
            if (!CanEquipItem(trinketItem, isRoll))
                return (null, null);

            ISheetSlot trinketSlot1 = _characterSheetManager.TrinketSlots[0];
            ISheetSlot trinketSlot2 = _characterSheetManager.TrinketSlots[1];

            if (trinketItem.UniqueEquipped)
            {
                if (trinketSlot1.GetItemLink == trinketItem.ItemLink
                    || trinketSlot2.GetItemLink == trinketItem.ItemLink)
                {
                    return (null, null);
                }
            }

            float trinket1Score = trinketSlot1.Item != null ? trinketSlot1.Item.WeightScore : 0;
            float trinket2Score = trinketSlot2.Item != null ? trinketSlot2.Item.WeightScore : 0;
            ISheetSlot lowestScoreTrinketSlot = trinket1Score <= trinket2Score ? trinketSlot1 : trinketSlot2;
            if (lowestScoreTrinketSlot.Item == null || lowestScoreTrinketSlot.Item.WeightScore < trinketItem.WeightScore)
            {
                string reason = lowestScoreTrinketSlot.Item == null ?
                    "Nothing equipped in this slot" : $"Replacing {lowestScoreTrinketSlot.Item.Name} ({lowestScoreTrinketSlot.Item.WeightScore})";
                return (lowestScoreTrinketSlot, reason);
            }
            return (null, null);
        }

        private void AutoEquipRanged()
        {
            ISheetSlot rangedSlot = _characterSheetManager.RangedSlot;
            List<IWIMItem> potentialRanged = GetEquipableFromBags(rangedSlot.InvTypes);
            foreach (IWIMItem rangedItem in potentialRanged)
            {
                string reasonToEquip = IsRangedBetter(rangedItem);
                if (reasonToEquip != null && EquipItem(rangedSlot, rangedItem, reasonToEquip))
                {
                    // TODO, update bags and sheet?
                }
            }
        }

        // returns the reason why the item should be equipped, null if the item is not better
        public string IsRangedBetter(IWIMItem rangedWeapon, bool isRoll = false)
        {
            if (!CanEquipItem(rangedWeapon, isRoll))
                return null;

            ISheetSlot rangedSlot = _characterSheetManager.RangedSlot;
            if (!AutoEquipSettings.CurrentSettings.SwitchRanged)
            {
                if (rangedWeapon.ItemSubType == "Guns" && !_containers.HaveBulletsInBags)
                {
                    return null;
                }
                if ((rangedWeapon.ItemSubType == "Crossbows" || rangedWeapon.ItemSubType == "Bows") && !_containers.HaveArrowsInBags)
                {
                    return null;
                }
            }

            bool itemIsBanned = _wantedRanged.ContainsKey(rangedWeapon.ItemSubType)
                && !_wantedRanged[rangedWeapon.ItemSubType];
            bool equippedIsBanned = rangedSlot.Item != null
                && _wantedRanged.ContainsKey(_characterSheetManager.RangedSlot.Item.ItemSubType)
                && !_wantedRanged[rangedSlot.Item.ItemSubType];

            // Switch because current item is unwanted
            if (equippedIsBanned && !itemIsBanned)
            {
                return $"You don't want {rangedSlot.Item.ItemSubType}";
            }

            // Skip because we have preferred ranged equipped
            if (itemIsBanned && rangedSlot.Item != null)
            {
                return null;
            }

            // Equip because slot is empty
            if (rangedSlot.Item == null)
            {
                return "Nothing equipped in this slot";
            }

            if (rangedSlot.Item.WeightScore < rangedWeapon.WeightScore)
            {
                if (itemIsBanned)
                {
                    return "Until we find a preferred option";
                }
                return $"Replacing {rangedSlot.Item.Name} ({rangedSlot.Item.WeightScore})";
            }
            return null;
        }

        private void AutoEquipAmmo()
        {
            ISheetSlot rangedSlot = _characterSheetManager.RangedSlot;
            ISheetSlot ammoSlot = _characterSheetManager.AmmoSlot;

            // List potential replacement for this slot
            List<IWIMItem> potentialAmmo = _containers.GetAllBagItems()
                .Where(i =>
                    i.ItemSubType == "Arrow" || i.ItemSubType == "Bullet"
                    && ObjectManager.Me.Level >= i.ItemMinLevel)
                .OrderBy(i => i.ItemMinLevel)
                .ToList();

            foreach (IWIMItem ammo in potentialAmmo)
            {
                string reasonToEquip = IsAmmoBetter(ammo, potentialAmmo);
                if (reasonToEquip != null && EquipItem(ammoSlot, ammo, reasonToEquip))
                {
                    // TODO, update bags and sheet?
                }
            }
        }

        // returns the reason why the item should be equipped, null if the item is not better
        public string IsAmmoBetter(IWIMItem ammo, List<IWIMItem> potentialAmmos)
        {
            ISheetSlot rangedSlot = _characterSheetManager.RangedSlot;
            ISheetSlot ammoSlot = _characterSheetManager.AmmoSlot;

            if (rangedSlot.Item == null)
            {
                return null;
            }

            string wantedAmmo = null;
            if (rangedSlot.Item.ItemSubType == "Crossbows" || rangedSlot.Item.ItemSubType == "Bows")
                wantedAmmo = "Arrow";
            if (rangedSlot.Item.ItemSubType == "Guns")
                wantedAmmo = "Bullet";

            // Not the right type of ammo
            if (wantedAmmo == null || wantedAmmo != ammo.ItemSubType)
            {
                return null;
            }

            if (ammoSlot.Item == null) return "Nothing equipped in this slot";
            if (ammoSlot.Item.ItemMinLevel > ammo.ItemMinLevel) return "Finishing lower level ammo first";
            if (ammoSlot.Item.ItemSubType != ammo.ItemSubType) return $"Switching to {ammo.ItemSubType}";
            if (!potentialAmmos.Exists(pa => pa.Name == ammoSlot.Item.Name)
                || !_containers.GetAllBagItems().Any(i => i.ItemId == ammo.ItemId))
                return $"We ran out of {ammoSlot.Item.Name}";

            return null;
        }

        private void AutoEquipWeapons()
        {
            List<IWIMItem> bagsWeapons = new List<IWIMItem>();
            bagsWeapons.AddRange(GetEquipableWeaponsFromBags(_characterSheetManager.WeaponSlots[0]));
            bagsWeapons.AddRange(GetEquipableWeaponsFromBags(_characterSheetManager.WeaponSlots[1]));

            foreach (IWIMItem weapon in bagsWeapons)
            {
                (ISheetSlot, string) slotAndReason = IsWeaponBetter(weapon);
                if (slotAndReason != (null, null) && EquipItem(slotAndReason.Item1, weapon, slotAndReason.Item2))
                {
                    _characterSheetManager.Scan();
                    _containers.Scan();
                }
            }
        }

        private void AddWeaponsToCombinations(Dictionary<(string, string), float> dic, string mainHand, string offHand, float score)
        {
            if (!dic.ContainsKey((mainHand, offHand)))
            {
                dic.Add((mainHand, offHand), score);
            }
        }

        public (ISheetSlot, string) IsWeaponBetter(IWIMItem weaponToCheck, bool isRoll = false)
        {
            ISheetSlot mainHandSlot = _characterSheetManager.WeaponSlots[0];
            ISheetSlot offHandSlot = _characterSheetManager.WeaponSlots[1];
            float unIdealDebuff = 0.3f;
            List<IWIMItem> listAllMainHandWeapons = GetEquipableWeaponsFromBags(mainHandSlot);
            List<IWIMItem> listAllOffHandWeapons = GetEquipableWeaponsFromBags(offHandSlot);

            if (mainHandSlot.Item != null)
            {
                listAllMainHandWeapons.Add(mainHandSlot.Item);
                if (offHandSlot.InvTypes.Contains(mainHandSlot.Item.ItemEquipLoc))
                {
                    listAllOffHandWeapons.Add(mainHandSlot.Item);
                }
            }

            if (offHandSlot.Item != null)
            {
                listAllOffHandWeapons.Add(offHandSlot.Item);
                if (mainHandSlot.InvTypes.Contains(offHandSlot.Item.ItemEquipLoc))
                {
                    listAllMainHandWeapons.Add(offHandSlot.Item);
                }
            }

            if (mainHandSlot.GetItemLink != weaponToCheck.ItemLink
                && mainHandSlot.InvTypes.Contains(weaponToCheck.ItemEquipLoc)
                && CanEquipItem(weaponToCheck, isRoll))
            {
                listAllMainHandWeapons.Add(weaponToCheck);
            }

            if (offHandSlot.GetItemLink != weaponToCheck.ItemLink
                && offHandSlot.InvTypes.Contains(weaponToCheck.ItemEquipLoc)
                && CanEquipItem(weaponToCheck, isRoll))
            {
                listAllOffHandWeapons.Add(weaponToCheck);
            }

            if (!_skillsManager.DualWield.KnownSpell)
            {
                listAllOffHandWeapons
                    .RemoveAll(weapon => weapon.ItemSubType != "Miscellaneous"
                        && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.Shield);
            }

            if (!_skillsManager.KnowTitansGrip)
            {
                listAllOffHandWeapons
                    .RemoveAll(weapon => TwoHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]));
            }

            listAllMainHandWeapons = listAllMainHandWeapons.OrderByDescending(w => w.WeightScore).ToList();
            listAllOffHandWeapons = listAllOffHandWeapons.OrderByDescending(w => w.WeightScore).ToList();

            Dictionary<(string, string), float> weaponCombinationsDic = new Dictionary<(string, string), float>();
            (string MainHand, string OffHand) bestCombintation = (null, null);
            float bestCombinationScore = 0;

            // try all combinations
            foreach (IWIMItem mainHandWeapon in listAllMainHandWeapons)
            {
                float mainHandWeaponScore = WeaponIsIdeal(mainHandWeapon) ? mainHandWeapon.WeightScore : mainHandWeapon.WeightScore * unIdealDebuff;

                // Two handers
                if (TwoHanders.Contains(ItemSkillsDictionary[mainHandWeapon.ItemSubType]))
                {
                    if (_skillsManager.KnowTitansGrip)
                    {
                        // Combine with offhand
                        foreach (IWIMItem offHandWeapon in listAllOffHandWeapons)
                        {
                            if (!mainHandWeapon.UniqueEquipped || mainHandWeapon.ItemLink != offHandWeapon.ItemLink)
                            {
                                float offHandWeaponScore = WeaponIsIdeal(offHandWeapon) ? offHandWeapon.WeightScore * 0.8f : offHandWeapon.WeightScore * unIdealDebuff;
                                AddWeaponsToCombinations(weaponCombinationsDic, mainHandWeapon.Name, offHandWeapon.Name, mainHandWeaponScore + offHandWeaponScore);
                            }
                        }
                    }
                    else
                    {
                        AddWeaponsToCombinations(weaponCombinationsDic, mainHandWeapon.Name, "NULL", mainHandWeaponScore);
                    }
                }
                // One handers
                if (OneHanders.Contains(ItemSkillsDictionary[mainHandWeapon.ItemSubType]))
                {
                    AddWeaponsToCombinations(weaponCombinationsDic, mainHandWeapon.Name, "NULL", mainHandWeaponScore);

                    // Combine with offhand
                    foreach (IWIMItem offHandWeapon in listAllOffHandWeapons)
                    {
                        if (!mainHandWeapon.UniqueEquipped || mainHandWeapon.ItemLink != offHandWeapon.ItemLink)
                        {
                            float offHandWeaponScore = WeaponIsIdeal(offHandWeapon) ? offHandWeapon.WeightScore * 0.8f : offHandWeapon.WeightScore * unIdealDebuff;
                            AddWeaponsToCombinations(weaponCombinationsDic, mainHandWeapon.Name, offHandWeapon.Name, mainHandWeaponScore + offHandWeaponScore);
                        }
                    }
                }
            }

            foreach (KeyValuePair<(string MainHand, string OffHand), float> combination in weaponCombinationsDic)
            {
                if (combination.Value > bestCombinationScore)
                {
                    bestCombintation = (combination.Key.MainHand, combination.Key.OffHand);
                    bestCombinationScore = combination.Value;
                }

                if (nbWeaponCombnations != weaponCombinationsDic.Count)
                {
                    Logger.Log($"New weapon combination {combination.Key.MainHand} + {combination.Key.OffHand} => {combination.Value}");
                }
            }

            if (nbWeaponCombnations != weaponCombinationsDic.Count)
            {
                Logger.Log($"Best is : {bestCombintation.MainHand} + {bestCombintation.OffHand} => {bestCombinationScore}");
            }

            nbWeaponCombnations = weaponCombinationsDic.Count;

            if (bestCombintation != (null, null))
            {
                if (weaponToCheck.Name == bestCombintation.MainHand
                    && mainHandSlot.GetItemLink != weaponToCheck.ItemLink)
                {
                    return (mainHandSlot, $"Better weapons combination score {bestCombinationScore}");
                }

                if (weaponToCheck.Name == bestCombintation.OffHand
                    && offHandSlot.GetItemLink != weaponToCheck.ItemLink)
                {
                    return (offHandSlot, $"Better weapons combination score {bestCombinationScore}");
                }
            }

            return (null, null);
        }

        /*
        // returns the slot and reason why the item should be equipped, (null, null) if the item is not better
        public (ISheetSlot, string) IsWeaponBetter(IWIMItem weaponToCheck, bool isRoll = false)
        {
            //Logger.LogDebug($"************ Weapon scan debug *****************");
            ISheetSlot mainHandSlot = _characterSheetManager.WeaponSlots[0];
            ISheetSlot offHandSlot = _characterSheetManager.WeaponSlots[1];
            float unIdealDebuff = 0.3f;
            
            bool currentWeaponsAreIdeal = WeaponIsIdeal(mainHandSlot.Item) && WeaponIsIdeal(offHandSlot.Item)
                || WeaponIsIdeal(mainHandSlot.Item) && offHandSlot.Item == null && AutoEquipSettings.CurrentSettings.EquipTwoHanders && !AutoEquipSettings.CurrentSettings.EquipOneHanders;   

            // Get current wepons combination score
            float currentMainHandScore = mainHandSlot.Item != null ? mainHandSlot.Item.WeightScore : 0f;
            float currentOffHandScore = offHandSlot.Item != null ? offHandSlot.Item.GetOffHandWeightScore() : 0f;
            float currentCombinedWeaponsScore = currentMainHandScore + currentOffHandScore;
            if (!currentWeaponsAreIdeal)
            {
                currentCombinedWeaponsScore = currentCombinedWeaponsScore * unIdealDebuff;
            }
            
            // Equip restricted to what we allow
            List<IWIMItem> listAllMainHandWeapons = GetEquipableWeaponsFromBags(mainHandSlot);
            List<IWIMItem> listAllOffHandWeapons = GetEquipableWeaponsFromBags(offHandSlot);

            if (mainHandSlot.Item != null)
            {
                listAllMainHandWeapons.Add(mainHandSlot.Item);
                if (offHandSlot.InvTypes.Contains(mainHandSlot.Item.ItemEquipLoc))
                {
                    listAllOffHandWeapons.Add(mainHandSlot.Item);
                }
            }

            if (offHandSlot.Item != null)
            {
                listAllOffHandWeapons.Add(offHandSlot.Item);
                if (mainHandSlot.InvTypes.Contains(offHandSlot.Item.ItemEquipLoc))
                {
                    listAllMainHandWeapons.Add(offHandSlot.Item);
                }
            }

            if (mainHandSlot.InvTypes.Contains(weaponToCheck.ItemEquipLoc) && CanEquipItem(weaponToCheck, isRoll))
            {
                listAllMainHandWeapons.Add(weaponToCheck);
            }

            if (offHandSlot.InvTypes.Contains(weaponToCheck.ItemEquipLoc) && CanEquipItem(weaponToCheck, isRoll))
            {
                listAllOffHandWeapons.Add(weaponToCheck);
            }

            Logger.LogError($"---------------------------------------");
            Logger.LogError($"CHECKING {weaponToCheck.Name}");
            if (!_skillsManager.DualWield.KnownSpell)
            {
                listAllOffHandWeapons
                    .RemoveAll(weapon => weapon.ItemSubType != "Miscellaneous"
                        && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.Shield);
            }

            if (!_skillsManager.KnowTitansGrip)
            {
                listAllOffHandWeapons
                    .RemoveAll(weapon => TwoHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]));
            }

            listAllMainHandWeapons = listAllMainHandWeapons.OrderByDescending(w => w.WeightScore).ToList();
            listAllOffHandWeapons = listAllOffHandWeapons.OrderByDescending(w => w.WeightScore).ToList();

            /*
            // Get ideal Two Hand
            IWIMItem ideal2H = listAllMainHandWeapons
                    .Where(w => TwoHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
                    .Where(w => WeaponIsIdeal(w))
                    .FirstOrDefault();

            // Get second choice Two Hand
            IWIMItem secondChoice2H = listAllMainHandWeapons
                    .Where(w => TwoHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
                    .Where(w => w != ideal2H)
                    .FirstOrDefault();

            // Get ideal Main hand
            IWIMItem idealMainhand = listAllMainHandWeapons
                .Where(w => OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType])
                    || SuitableForTitansGrips(w))
                .Where(w => WeaponIsIdeal(w) || mainHandSlot.Item == null)
                .FirstOrDefault();

            // Get Second choice Main hand
            IWIMItem secondChoiceMainhand = listAllMainHandWeapons
                .Where(w => OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType]) || SuitableForTitansGrips(w))
                .Where(w => w != idealMainhand)
                .FirstOrDefault();

            // Swap if both ideal are One Hand/Main Hand
            if (idealMainhand != null
                && secondChoiceMainhand != null
                && secondChoiceMainhand.WeightScore > idealMainhand.WeightScore
                && idealMainhand.ItemLink != secondChoiceMainhand.ItemLink
                && idealMainhand.ItemEquipLoc == "INVTYPE_WEAPON"
                && secondChoiceMainhand.ItemEquipLoc == "INVTYPE_WEAPONMAINHAND")
            {
                IWIMItem first = idealMainhand;
                idealMainhand = secondChoiceMainhand;
                secondChoiceMainhand = first;
            }

            // Get ideal OffHand
            IWIMItem idealOffHand = listAllOffHandWeapons
                .Where(w => WeaponIsIdeal(w) || offHandSlot.Item == null || !WeaponIsIdeal(offHandSlot.Item) && offHandSlot.Item != w)
                .Where(w => _skillsManager.DualWield.KnownSpell
                    || ItemSkillsDictionary[w.ItemSubType] == SkillLine.Shield
                    || !_skillsManager.DualWield.KnownSpell && w.ItemSubType == "Miscellaneous"
                    || SuitableForTitansGrips(w))
                .Where(w => w != idealMainhand && w != ideal2H)
                .FirstOrDefault();

            // Get second choice OffHand
            IWIMItem secondChoiceOffhand = listAllOffHandWeapons
                .Where(w => w.ItemSubType == "Miscellaneous"
                    || (_skillsManager.DualWield.KnownSpell && OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
                    || SuitableForTitansGrips(w))
                .Where(w => _skillsManager.DualWield.KnownSpell
                    || ItemSkillsDictionary[w.ItemSubType] == SkillLine.Shield
                    || !_skillsManager.DualWield.KnownSpell && w.ItemSubType == "Miscellaneous")
                .Where(w => w != secondChoiceMainhand)
                .FirstOrDefault();

            float scoreIdealMainHand = idealMainhand == null ? 0 : idealMainhand.WeightScore;
            float scoreIdealOffhand = idealOffHand == null ? 0 : idealOffHand.GetOffHandWeightScore();

            float scoreSecondChoiceMainHand = secondChoiceMainhand == null ? 0 : secondChoiceMainhand.WeightScore * unIdealDebuff;
            float scoreSecondOffhand = secondChoiceOffhand == null ? 0 : secondChoiceOffhand.GetOffHandWeightScore() * unIdealDebuff;

            float finalScore2hander = ideal2H == null ? 0 : ideal2H.WeightScore;
            float finalScoreDualWield = scoreIdealMainHand + scoreIdealOffhand;

            float finalScoreSecondChoice2hander = secondChoice2H == null ? 0 : secondChoice2H.WeightScore * unIdealDebuff;
            float finalScoreSecondDualWield = (scoreSecondChoiceMainHand + scoreSecondOffhand) * unIdealDebuff;
            */
        /*
        Logger.LogDebug($"Current is preffered : {currentWeaponsAreIdeal} ({currentCombinedWeaponsScore})");
        Logger.LogDebug($"2H 1 {ideal2H?.Name} ({finalScore2hander}) -- 2H 2 {secondChoice2H?.Name} ({finalScoreSecondChoice2hander})");
        Logger.LogDebug($"1H 1 {idealMainhand?.Name} ({scoreIdealMainHand}) -- 1H 2 {secondChoiceMainhand?.Name} ({scoreSecondChoiceMainHand})");
        Logger.LogDebug($"OFFHAND 1 {idealOffHand?.Name} ({scoreIdealOffhand}) -- OFFHAND 2 {secondChoiceOffhand?.Name} ({scoreSecondOffhand})");
        Logger.LogDebug($"COMBINED 1 {idealMainhand?.Name} + {idealOffHand?.Name} ({finalScoreDualWield}) -- COMBINED 2 {secondChoiceMainhand?.Name} + {secondChoiceOffhand?.Name} ({finalScoreSecondDualWield})");
        */
        /*
        float[] scores = new float[4] { finalScore2hander, finalScoreSecondChoice2hander, finalScoreDualWield, finalScoreSecondDualWield };
        float bestScore = scores.Max();

        // One of the scores is better
        if (bestScore > currentCombinedWeaponsScore)
        {
            // Main 2 hander
            if (bestScore == scores[0] && ideal2H == weaponToCheck)
                return (mainHandSlot, $"Better 2H score {finalScore2hander}/{currentCombinedWeaponsScore}");
            // Second choice 2 hander
            if (bestScore == scores[1] && secondChoice2H == weaponToCheck)
                return (mainHandSlot, $"Better 2H score (unideal) {finalScoreSecondChoice2hander}/{currentCombinedWeaponsScore}");
            // Main dual wield
            if (bestScore == scores[2] && idealMainhand == weaponToCheck)
                return (mainHandSlot, $"Better MH for combined score {finalScoreDualWield}/{currentCombinedWeaponsScore}");
            if (bestScore == scores[2] && idealOffHand == weaponToCheck && idealMainhand != null)
                return (offHandSlot, $"Better OH for combined score {finalScoreDualWield}/{currentCombinedWeaponsScore}");
            // Second choice dual wield
            if (bestScore == scores[3] && secondChoiceMainhand == weaponToCheck)
                return (mainHandSlot, $"Better MH (unideal) for combined score {finalScoreSecondDualWield}/{currentCombinedWeaponsScore}");
            if (bestScore == scores[3] && secondChoiceOffhand == weaponToCheck && secondChoiceMainhand != null)
                return (offHandSlot, $"Better OH (unideal) for combined score {finalScoreSecondDualWield}/{currentCombinedWeaponsScore}");
        }

        return (null, null);

    }
        */

        private bool WeaponIsIdeal(IWIMItem weapon)
        {
            if (weapon == null || !ItemSkillsDictionary.ContainsKey(weapon.ItemSubType))
                return false;

            if (ClassSpecManager.MySpec == ClassSpec.RogueAssassination
                && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.Daggers)
                return false;

            // Shields
            if (ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.Shield)
            {
                return AutoEquipSettings.CurrentSettings.EquipShields;
            }

            // Two handers
            if (TwoHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]))
            {
                // Titan's grip
                if (_skillsManager.KnowTitansGrip
                    && (ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedSwords
                    || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedAxes
                    || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedMaces))
                {
                    return true;
                }
                // Normal
                return AutoEquipSettings.CurrentSettings.EquipTwoHanders;
            }

            // One handers
            if (OneHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]))
            {
                return AutoEquipSettings.CurrentSettings.EquipOneHanders;
            }

            return false;
        }

        private bool SuitableForTitansGrips(IWIMItem weapon)
        {
            return _skillsManager.KnowTitansGrip
                && (ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedSwords
                || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedAxes
                || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedMaces);
        }

        private List<IWIMItem> GetEquipableWeaponsFromBags(ISheetSlot slot)
        {
            return _containers.GetAllBagItems()
                .Where(i =>
                    CanEquipItem(i)
                    && (slot.InvTypes.Contains(i.ItemEquipLoc) || SuitableForTitansGrips(i)))
                .ToList();
        }

        private void CheckSwapWeapons()
        {
            ISheetSlot mainHandSlot = _characterSheetManager.WeaponSlots[0];
            ISheetSlot offHandSlot = _characterSheetManager.WeaponSlots[1];
            if (mainHandSlot.Item?.ItemEquipLoc == "INVTYPE_WEAPON"
                && offHandSlot.Item?.ItemEquipLoc == "INVTYPE_WEAPON"
                && mainHandSlot.Item.WeaponSpeed < offHandSlot.Item.WeaponSpeed)
            {
                Logger.Log("Swapping weapons to have slower speed in main hand");
                mainHandSlot.Item.ClickInInventory(mainHandSlot.InventorySlotID);
                mainHandSlot.Item.ClickInInventory(offHandSlot.InventorySlotID);
            }
        }

        private bool CanEquipItem(IWIMItem item, bool isRoll = false)
        {
            if (item.ItemSubType == ""
                || !Conditions.InGameAndConnectedAndProductStartedNotInPause
                || (!ItemSkillsDictionary.ContainsKey(item.ItemSubType) && item.ItemSubType != "Miscellaneous"))
            {
                return false;
            }

            bool skillCheckOK = item.ItemSubType == "Miscellaneous"
                || _skillsManager.MySkills.ContainsKey(item.ItemSubType) && _skillsManager.MySkills[item.ItemSubType] > 0
                || item.ItemSubType == "Fist Weapons" && Skill.Has(SkillLine.FistWeapons);

            bool isLevelOK = ObjectManager.Me.Level >= item.ItemMinLevel || isRoll;

            return isLevelOK && skillCheckOK && GetNbEquipAttempts(item.ItemLink) < _maxNbEquipAttempts;
        }

        private List<IWIMItem> GetEquipableFromBags(string[] invTypes)
        {
            return _containers.GetAllBagItems()
                .Where(i =>
                    invTypes.Contains(i.ItemEquipLoc)
                    && CanEquipItem(i))
                .OrderByDescending(i => i.WeightScore)
                .ToList();
        }

        private List<IWIMItem> GetEquipableFromBags(string invType)
        {
            return _containers.GetAllBagItems()
                .Where(i =>
                    invType == i.ItemEquipLoc
                    && CanEquipItem(i))
                .OrderByDescending(i => i.WeightScore)
                .ToList();
        }

        private bool EquipItem(ISheetSlot sheetSlot, IWIMItem item, string reason)
        {
            _lootFilter.ProtectFromFilter(item.ItemLink);

            if (sheetSlot.Item?.ItemLink == item.ItemLink)
            {
                _lootFilter.AllowForFilter(item.ItemLink);
                return true;
            }

            if (item.ItemSubType != "Arrow"
                && item.ItemSubType != "Bullet"
                && (ObjectManager.Me.InCombatFlagOnly || ObjectManager.Me.IsCast))
            {
                return false;
            }

            if (item.BagIndex < 0 || item.SlotIndex < 0)
            {
                Logger.LogError($"Item {item.Name} is not recorded as being in a bag. Can't use.");
            }
            else
            {
                Logger.Log($"Equipping {item.Name} ({item.WeightScore}) [{reason}]");
                _itemEquipAttempts.Add(item.ItemLink);

                Lua.LuaDoString($@"
                    ClearCursor();
                    PickupContainerItem({item.BagIndex}, {item.SlotIndex});
                    EquipCursorItem({sheetSlot.InventorySlotID});
                    EquipPendingItem(0);
                ");

                ToolBox.Sleep(200); // wait for UI to update

                int itemIdInSlot = Lua.LuaDoString<int>($@"return GetInventoryItemID(""player"", {sheetSlot.InventorySlotID})");

                _characterSheetManager.Scan();
                _containers.Scan();

                //if (sheetSlot.Item == null || sheetSlot.Item.ItemLink != item.ItemLink)
                if (itemIdInSlot == 0 || itemIdInSlot != item.ItemId)
                {
                    int nbEquipItem = GetNbEquipAttempts(item.ItemLink);
                    if (nbEquipItem < _maxNbEquipAttempts)
                    {
                        Logger.LogError($"Failed to equip {item.Name}. Retrying soon ({nbEquipItem}/{_maxNbEquipAttempts}).");
                    }
                    else
                    {
                        Logger.LogError($"Failed to equip {item.Name} after {nbEquipItem} attempts.");
                    }

                    Lua.LuaDoString($"ClearCursor()");
                    return false;
                }
                _itemEquipAttempts.Remove(item.ItemLink);
                _lootFilter.AllowForFilter(item.ItemLink);
                return true;

            }
            return false;
        }

        private int GetNbEquipAttempts(string itemLink) => _itemEquipAttempts.Where(i => i == itemLink).Count();
    }
}
