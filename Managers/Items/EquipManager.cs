using robotManager.Helpful;
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
        private Timer _bagUpdateTimer = new Timer();
        private bool _bagShouldUpdate = false;
        private readonly Dictionary<(string, string), float> _weaponCombinationsDic = new Dictionary<(string, string), float>();

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
            if (id == "BAG_UPDATE")
            {
                _bagShouldUpdate = true;
            }

            if (_bagShouldUpdate == true && _bagUpdateTimer.IsReady)
            {
                CheckAll();
                _bagShouldUpdate = false;
                _bagUpdateTimer = new Timer(200); // avoid bag update spam
            }

            if (id == "PLAYER_REGEN_ENABLED"
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
            if (isRoll
                && _containers.GetAllBagItems().ToList().Exists(item => item.ItemLink == armorItem.ItemLink))
            {
                return (null, null);
            }
            
            if (!CanEquipItem(armorItem, isRoll))
            {
                return (null, null);
            }

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
            if (isRoll
                && _containers.GetAllBagItems().ToList().Exists(item => item.ItemLink == ringItem.ItemLink))
            {
                return (null, null);
            }

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
            if (isRoll
                && _containers.GetAllBagItems().ToList().Exists(item => item.ItemLink == trinketItem.ItemLink))
            {
                return (null, null);
            }

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
            if (isRoll
                && _containers.GetAllBagItems().ToList().Exists(item => item.ItemLink == rangedWeapon.ItemLink))
            {
                return null;
            }

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
            if (isRoll
                && _containers.GetAllBagItems().ToList().Exists(item => item.ItemLink == weaponToCheck.ItemLink))
            {
                return (null, null);
            }

            ISheetSlot mainHandSlot = _characterSheetManager.WeaponSlots[0];
            ISheetSlot offHandSlot = _characterSheetManager.WeaponSlots[1];
            float unIdealDebuff = 0.3f;

            // first get all weapons in bags
            List<IWIMItem> allEquippableWeapons = _containers.GetAllBagItems()
                .Where(weapon =>
                    CanEquipItem(weapon)
                    && (mainHandSlot.InvTypes.Contains(weapon.ItemEquipLoc) || offHandSlot.InvTypes.Contains(weapon.ItemEquipLoc) || SuitableForTitansGrips(weapon)))
                .ToList();

            // Add current MH
            if (mainHandSlot.Item != null)
            {
                allEquippableWeapons.Add(mainHandSlot.Item);
            }
            // Add current OH
            if (offHandSlot.Item != null)
            {
                allEquippableWeapons.Add(offHandSlot.Item);
            }
            // If roll, add roll item
            if (isRoll 
                && CanEquipItem(weaponToCheck, true) 
                && !allEquippableWeapons.Exists(w => w.ItemLink == weaponToCheck.ItemLink))
            {
                allEquippableWeapons.Add(weaponToCheck);
            }

            List<IWIMItem> listAllMainHandWeapons = GetEquipableWeaponsFromBags(mainHandSlot);
            List<IWIMItem> listAllOffHandWeapons = GetEquipableWeaponsFromBags(offHandSlot);
            foreach (IWIMItem weapon in allEquippableWeapons)
            {
                if (mainHandSlot.InvTypes.Contains(weapon.ItemEquipLoc))
                {
                    listAllMainHandWeapons.Add(weapon);
                }
                if (offHandSlot.InvTypes.Contains(weapon.ItemEquipLoc))
                {
                    listAllOffHandWeapons.Add(weapon);
                }
            }

            if (!_skillsManager.KnowTitansGrip)
            {
                listAllOffHandWeapons
                    .RemoveAll(weapon => TwoHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]));
            }

            listAllMainHandWeapons = listAllMainHandWeapons
                .OrderBy(weapon => weapon.ItemLink)
                .OrderByDescending(w => w.WeightScore)
                .ToList();
            listAllOffHandWeapons = listAllOffHandWeapons
                .OrderBy(weapon => weapon.ItemLink)
                .OrderByDescending(w => w.WeightScore)
                .ToList();

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
                            bool canEquip2SameWeapons = !mainHandWeapon.UniqueEquipped 
                                && allEquippableWeapons.Count(item => item.ItemLink == offHandWeapon.ItemLink) > 1;
                            if (mainHandWeapon.ItemLink != offHandWeapon.ItemLink || canEquip2SameWeapons)
                            {
                                float offHandWeaponScore = WeaponIsIdeal(offHandWeapon) ? offHandWeapon.WeightScore * 0.8f : offHandWeapon.WeightScore * unIdealDebuff;
                                AddWeaponsToCombinations(_weaponCombinationsDic, mainHandWeapon.Name, offHandWeapon.Name, mainHandWeaponScore + offHandWeaponScore);
                            }
                        }
                    }
                    else
                    {
                        AddWeaponsToCombinations(_weaponCombinationsDic, mainHandWeapon.Name, "NULL", mainHandWeaponScore);
                    }
                }
                // One handers
                if (OneHanders.Contains(ItemSkillsDictionary[mainHandWeapon.ItemSubType]))
                {
                    AddWeaponsToCombinations(_weaponCombinationsDic, mainHandWeapon.Name, "NULL", mainHandWeaponScore);

                    // Combine with offhand
                    foreach (IWIMItem offHandWeapon in listAllOffHandWeapons)
                    {
                        bool canEquip2SameWeapons = !mainHandWeapon.UniqueEquipped 
                            && allEquippableWeapons.Count(item => item.ItemLink == offHandWeapon.ItemLink) > 1;
                        if (mainHandWeapon.ItemLink != offHandWeapon.ItemLink || canEquip2SameWeapons)
                        {
                            float offHandWeaponScore = WeaponIsIdeal(offHandWeapon) ? offHandWeapon.WeightScore * 0.8f : offHandWeapon.WeightScore * unIdealDebuff;
                            AddWeaponsToCombinations(_weaponCombinationsDic, mainHandWeapon.Name, offHandWeapon.Name, mainHandWeaponScore + offHandWeaponScore);
                        }
                    }
                }
            }

            foreach (KeyValuePair<(string MainHand, string OffHand), float> combination in _weaponCombinationsDic)
            {
                if (combination.Value > bestCombinationScore)
                {
                    bestCombintation = (combination.Key.MainHand, combination.Key.OffHand);
                    bestCombinationScore = combination.Value;
                }

                if (nbWeaponCombnations != _weaponCombinationsDic.Count)
                {
                    Logger.Log($"New weapon combination {combination.Key.MainHand} + {combination.Key.OffHand} => {combination.Value}");
                }
            }

            if (nbWeaponCombnations != _weaponCombinationsDic.Count)
            {
                Logger.Log($"Best is : {bestCombintation.MainHand} + {bestCombintation.OffHand} => {bestCombinationScore}");
            }

            nbWeaponCombnations = _weaponCombinationsDic.Count;

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
