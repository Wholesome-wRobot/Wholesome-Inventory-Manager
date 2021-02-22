using System;
using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

public static class WAECharacterSheet
{
    public static WAECharacterSheetSlot Ammo { get; set; } = new WAECharacterSheetSlot(0, new string[] { "INVTYPE_AMMO" });
    public static WAECharacterSheetSlot Head { get; set; } = new WAECharacterSheetSlot(1, new string[] { "INVTYPE_HEAD" });
    public static WAECharacterSheetSlot Neck { get; set; } = new WAECharacterSheetSlot(2, new string[] { "INVTYPE_NECK" });
    public static WAECharacterSheetSlot Shoulder { get; set; } = new WAECharacterSheetSlot(3, new string[] { "INVTYPE_SHOULDER" });
    public static WAECharacterSheetSlot Chest { get; set; } = new WAECharacterSheetSlot(5, new string[] { "INVTYPE_CHEST", "INVTYPE_ROBE" });
    public static WAECharacterSheetSlot Waist { get; set; } = new WAECharacterSheetSlot(6, new string[] { "INVTYPE_WAIST" });
    public static WAECharacterSheetSlot Legs { get; set; } = new WAECharacterSheetSlot(7, new string[] { "INVTYPE_LEGS" });
    public static WAECharacterSheetSlot Feet { get; set; } = new WAECharacterSheetSlot(8, new string[] { "INVTYPE_FEET" });
    public static WAECharacterSheetSlot Wrist { get; set; } = new WAECharacterSheetSlot(9, new string[] { "INVTYPE_WRIST" });
    public static WAECharacterSheetSlot Hands { get; set; } = new WAECharacterSheetSlot(10, new string[] { "INVTYPE_HAND" });
    public static WAECharacterSheetSlot Finger1 { get; set; } = new WAECharacterSheetSlot(11, new string[] { "INVTYPE_FINGER" });
    public static WAECharacterSheetSlot Finger2 { get; set; } = new WAECharacterSheetSlot(12, new string[] { "INVTYPE_FINGER" });
    public static WAECharacterSheetSlot Trinket1 { get; set; } = new WAECharacterSheetSlot(13, new string[] { "INVTYPE_TRINKET" });
    public static WAECharacterSheetSlot Trinket2 { get; set; } = new WAECharacterSheetSlot(14, new string[] { "INVTYPE_TRINKET" });
    public static WAECharacterSheetSlot Back { get; set; } = new WAECharacterSheetSlot(15, new string[] { "INVTYPE_CLOAK" });
    public static WAECharacterSheetSlot MainHand { get; set; } = new WAECharacterSheetSlot(16, new string[] { "INVTYPE_WEAPON", "INVTYPE_WEAPONMAINHAND", "INVTYPE_2HWEAPON" });
    public static WAECharacterSheetSlot OffHand { get; set; } = new WAECharacterSheetSlot(17, new string[] { "INVTYPE_WEAPON", "INVTYPE_SHIELD", "INVTYPE_HOLDABLE", "INVTYPE_WEAPONOFFHAND" });
    public static WAECharacterSheetSlot Ranged { get; set; } = new WAECharacterSheetSlot(18, new string[] { "INVTYPE_RANGEDRIGHT", "INVTYPE_RANGED", "INVTYPE_THROWN" });
    public static List<string> AllItemLinks { get; set; } = new List<string>();
    public static Dictionary<string, int> MySkills { get; set; } = new Dictionary<string, int>();
    public static ClassSpec ClassSpec { get; set; }

    // Spells
    private static Spell DualWield;
    private static bool KnowTitansGrip;

    public static void Scan()
    {
        //Logger.LogDebug("*** Scanning character sheet...");
        DateTime dateBegin = DateTime.Now;

        RecordKnownSpecialSkills();

        AllItemLinks.Clear();
        AllItemLinks = Lua.LuaDoString<string>($@"
                                local allItems = """";
                                for i=0, 19 do
                                    local item = GetInventoryItemLink(""player"", i);
                                    if item == nil then item = ""null"" end;
                                    allItems = allItems..'$'..item;
                                end
                                return allItems;").Split('$').ToList();
        AllItemLinks.RemoveAt(0);

        foreach (WAECharacterSheetSlot slot in AllSlots)
            slot.RefreshItem();

        //Logger.LogDebug($"CharSheet Scan Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    private static void AutoEquipAmor()
    {
        foreach (WAECharacterSheetSlot armorSlot in ArmorSlots)
        {
            //Logger.LogDebug($"{armorSlot.InventorySlotID} -> {armorSlot.InvTypes} -> {armorSlot.Item?.Name} ({armorSlot.Item?.WeightScore})");
            // List potential replacement for this slot
            List<WAEItem> potentialArmors = WAEContainers.AllItems
                .FindAll(i =>
                    armorSlot.InvTypes.Contains(i.ItemEquipLoc)
                    && i.CanEquip())
                .OrderByDescending(i => i.WeightScore)
                .ToList();

            foreach (WAEItem item in potentialArmors)
            {
                //Logger.LogDebug($"Potential item: {item.Name} ({item.WeightScore})");
                if (armorSlot.Item == null || armorSlot.Item.WeightScore < item.WeightScore)
                {
                    if (armorSlot.Item == null)
                        Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                    else
                        Logger.Log($"Replacing {armorSlot.Item.Name} ({armorSlot.Item.WeightScore}) with {item.Name} ({item.WeightScore})");

                    if (item.Equip(armorSlot.InventorySlotID))
                        break;
                }
            }
        }
    }

    private static void AutoEquipRings()
    {
        float ring1Score = Finger1.Item != null ? Finger1.Item.WeightScore : 0;
        float ring2Score = Finger2.Item != null ? Finger2.Item.WeightScore : 0;
        WAECharacterSheetSlot lowestScoreFingerSlot = ring1Score <= ring2Score ? Finger1 : Finger2;

        // List potential replacement for this slot
        List<WAEItem> potentialRings = WAEContainers.AllItems
            .FindAll(i =>
                i.ItemEquipLoc == "INVTYPE_FINGER"
                && i.CanEquip())
            .OrderByDescending(i => i.WeightScore)
            .ToList();

        foreach (WAEItem item in potentialRings)
        {
            //Logger.LogDebug($"Potential Ring: {item.Name} ({item.WeightScore})");
            if (lowestScoreFingerSlot.Item == null
                || lowestScoreFingerSlot.Item.WeightScore < item.WeightScore)
            {
                if (lowestScoreFingerSlot.Item == null)
                    Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                else
                    Logger.Log($"Replacing {lowestScoreFingerSlot.Item.Name} ({lowestScoreFingerSlot.Item.WeightScore}) with {item.Name} ({item.WeightScore})");

                if (item.Equip(lowestScoreFingerSlot.InventorySlotID))
                    break;
            }
        }
    }

    private static void AutoEquipTrinkets()
    {
        float trinket1Score = Trinket1.Item != null ? Trinket1.Item.WeightScore : 0;
        float trinket2Score = Trinket2.Item != null ? Trinket2.Item.WeightScore : 0;
        WAECharacterSheetSlot lowestScoreTrinketSlot = trinket1Score <= trinket2Score ? Trinket1 : Trinket2;

        // List potential replacement for this slot
        List<WAEItem> potentialTrinkets = WAEContainers.AllItems
            .FindAll(i =>
                i.ItemEquipLoc == "INVTYPE_TRINKET"
                && i.CanEquip())
            .OrderByDescending(i => i.WeightScore)
            .ToList();

        foreach (WAEItem item in potentialTrinkets)
        {
            //Logger.LogDebug($"Potential Trinket: {item.Name} ({item.WeightScore})");
            if (lowestScoreTrinketSlot.Item == null
                || lowestScoreTrinketSlot.Item.WeightScore < item.WeightScore)
            {
                if (lowestScoreTrinketSlot.Item == null)
                    Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                else
                    Logger.Log($"Replacing {lowestScoreTrinketSlot.Item.Name} ({lowestScoreTrinketSlot.Item.WeightScore}) with {item.Name} ({item.WeightScore})");

                if (item.Equip(lowestScoreTrinketSlot.InventorySlotID))
                    break;
            }
        }
    }

    private static void AutoEquipRanged()
    {
        bool haveBulletsInBags = WAEContainers.AllItems.Exists(i => i.ItemSubType == "Bullet" && ObjectManager.Me.Level >= i.ItemMinLevel);
        bool haveArrowsInBags = WAEContainers.AllItems.Exists(i => i.ItemSubType == "Arrow" && ObjectManager.Me.Level >= i.ItemMinLevel);
        bool noAmmoForMyCurrentRanged = Ranged.Item?.ItemSubType == "Crossbows" && !haveArrowsInBags
                                        || Ranged.Item?.ItemSubType == "Bows" && !haveArrowsInBags
                                        || Ranged.Item?.ItemSubType == "Guns" && !haveBulletsInBags;

        // List potential replacement for this slot
        List<WAEItem> potentialRanged = WAEContainers.AllItems
            .FindAll(i =>
                Ranged.InvTypes.Contains(i.ItemEquipLoc)
                && i.CanEquip())
            .OrderByDescending(i => i.WeightScore)
            .ToList();

        foreach (WAEItem item in potentialRanged)
        {
            if (!AutoEquipSettings.CurrentSettings.SwitchRanged)
            {
                if (item.ItemSubType == "Guns" && !haveBulletsInBags)
                    continue;
                if ((item.ItemSubType == "Crossbows" || item.ItemSubType == "Bows") && !haveArrowsInBags)
                    continue;
            }

            bool itemTypeIsBanned = Main.WantedItemType.ContainsKey(item.ItemSubType) && !Main.WantedItemType[item.ItemSubType];
            bool equippedItemIsBanned = Ranged.Item != null
                && Main.WantedItemType.ContainsKey(Ranged.Item.ItemSubType)
                && !Main.WantedItemType[Ranged.Item.ItemSubType];

            if (equippedItemIsBanned && !itemTypeIsBanned)
            {
                Logger.Log($"Equipping {item.Name} ({item.WeightScore}) because your don't want {Ranged.Item.ItemSubType}");
                item.Equip(Ranged.InventorySlotID);
                continue;
            }

            if (itemTypeIsBanned && Ranged.Item != null)
                continue;

            // Equip because slot is empty
            if (Ranged.Item == null)
            {
                Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                if (item.Equip(Ranged.InventorySlotID))
                    break;
            }

            if ((Ranged.Item.WeightScore < item.WeightScore)
                && (noAmmoForMyCurrentRanged || AutoEquipSettings.CurrentSettings.SwitchRanged))
            {
                if (itemTypeIsBanned)
                    Logger.Log($"Equipping {item.Name} ({item.WeightScore}) until we find a better option");
                else
                    Logger.Log($"Replacing {Ranged.Item.Name} ({Ranged.Item.WeightScore}) with {item.Name} ({item.WeightScore})");

                if (item.Equip(Ranged.InventorySlotID))
                    break;
            }
        }
    }

    private static void AutoEquipAmmo()
    {
        if (Ranged.Item != null)
        {
            string typeAmmo = null;
            if (Ranged.Item.ItemSubType == "Crossbows" || Ranged.Item.ItemSubType == "Bows")
                typeAmmo = "Arrow";
            if (Ranged.Item.ItemSubType == "Guns")
                typeAmmo = "Bullet";

            // List potential replacement for this slot
            List<WAEItem> potentialAmmo = WAEContainers.AllItems
                .FindAll(i =>
                    i.ItemSubType == typeAmmo
                    && ObjectManager.Me.Level >= i.ItemMinLevel)
                .OrderByDescending(i => i.ItemMinLevel)
                .ToList();

            foreach (WAEItem item in potentialAmmo)
            {
                //Logger.LogDebug($"Potential ammo: {item.Name} ({item.ItemMinLevel})");

                if (Ammo.Item == null
                    || Ammo.Item.ItemMinLevel < item.ItemMinLevel
                    || Ammo.Item.ItemSubType != item.ItemSubType
                    || !WAEContainers.AllItems.Exists(i => i.ItemId == item.ItemId))
                {
                    if (Ammo.Item == null)
                        Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                    else
                        Logger.Log($"Replacing {Ammo.Item.Name} with {item.Name}");

                    if (item.Equip(Ammo.InventorySlotID))
                        break;
                }
            }
        }
    }

    private static void AutoEquipWeapons()
    {
        //Logger.LogDebug($"************ Weapon scan debug *****************");
        bool currentWeaponsAreIdeal = WeaponIsIdeal(MainHand.Item) && WeaponIsIdeal(OffHand.Item)
            || WeaponIsIdeal(MainHand.Item) && OffHand.Item == null && AutoEquipSettings.CurrentSettings.EquipTwoHanders && !AutoEquipSettings.CurrentSettings.EquipOneHanders;
        float unIdealDebuff = 0.6f;

        float currentMainHandScore = MainHand.Item != null ? MainHand.Item.WeightScore : 0f;
        float currentOffHandScore = OffHand.Item != null ? OffHand.Item.GetOffHandWeightScore() : 0f;
        float currentCombinedWeaponsScore = currentMainHandScore + currentOffHandScore;
        if (!currentWeaponsAreIdeal)
            currentCombinedWeaponsScore = currentCombinedWeaponsScore * unIdealDebuff;

        // Equip restricted to what we allow
        List<WAEItem> listAllMainHandWeapons = GetEquipableWeaponsFromBags(MainHand);

        if (MainHand.Item != null) listAllMainHandWeapons.Add(MainHand.Item);
        List<WAEItem> listAllOffHandWeapons = GetEquipableWeaponsFromBags(OffHand);
        if (OffHand.Item != null) listAllOffHandWeapons.Add(OffHand.Item);

        listAllMainHandWeapons = listAllMainHandWeapons.OrderByDescending(w => w.WeightScore).ToList();
        listAllOffHandWeapons = listAllOffHandWeapons.OrderByDescending(w => w.WeightScore).ToList();

        // Get ideal Two Hand
        WAEItem ideal2H = listAllMainHandWeapons
                .Where(w => TwoHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
                .Where(weapon => WeaponIsIdeal(weapon))
                .FirstOrDefault();

        // Get second choice Two Hand
        WAEItem secondChoice2H = listAllMainHandWeapons
                .Where(w => TwoHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
                .Where(w => w != ideal2H)
                .FirstOrDefault();

        // Get ideal Main hand
        WAEItem idealMainhand = listAllMainHandWeapons
            .Where(w => OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType])
                || SuitableForTitansGrips(w))
            .Where(weapon => WeaponIsIdeal(weapon) || MainHand.Item == null)
            .FirstOrDefault();

        // Get Second choice Main hand
        WAEItem secondChoiceMainhand = listAllMainHandWeapons
            .Where(w => OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType]) || SuitableForTitansGrips(w))
            .Where(w => w != idealMainhand)
            .FirstOrDefault();

        // Get ideal OffHand
        WAEItem idealOffHand = listAllOffHandWeapons
            .Where(w => w.ItemSubType == "Miscellaneous" || OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType]))
            .Where(w => WeaponIsIdeal(w) || OffHand.Item == null)
            .Where(w => DualWield.KnownSpell
            || ItemSkillsDictionary[w.ItemSubType] == SkillLine.Shield
            || !DualWield.KnownSpell && w.ItemSubType == "Miscellaneous")
            .Where(w => w != idealMainhand && w != ideal2H)
            .FirstOrDefault();

        // Get second choice OffHand
        WAEItem secondChoiceOffhand = listAllOffHandWeapons
            .Where(w => (w.ItemSubType == "Miscellaneous" || DualWield.KnownSpell)
                || OneHanders.Contains(ItemSkillsDictionary[w.ItemSubType])
                || SuitableForTitansGrips(w))
            .Where(w => DualWield.KnownSpell
            || ItemSkillsDictionary[w.ItemSubType] == SkillLine.Shield
            || !DualWield.KnownSpell && w.ItemSubType == "Miscellaneous")
            .Where(w => w != secondChoiceMainhand)
            .FirstOrDefault();

        float scoreIdealMainHand = idealMainhand == null ? 0 : idealMainhand.WeightScore;
        float scoreIdealOffhand = idealOffHand == null ? 0 : idealOffHand.GetOffHandWeightScore();

        float scoreSecondChoiceMainHand = secondChoiceMainhand == null ? 0 : secondChoiceMainhand.WeightScore;
        float scoreSecondOffhand = secondChoiceOffhand == null ? 0 : secondChoiceOffhand.GetOffHandWeightScore();

        float finalScore2hands = ideal2H == null ? 0 : ideal2H.WeightScore;
        float finalScoreDualWield = scoreIdealMainHand + scoreIdealOffhand;

        float finalScoreSecondDualWield = (scoreSecondChoiceMainHand + scoreSecondOffhand) * unIdealDebuff;
        float finalScoreSecondChoice2hands = secondChoice2H == null ? 0 : secondChoice2H.WeightScore * unIdealDebuff;

        Logger.LogDebug($"Current is preffered : {currentWeaponsAreIdeal} ({currentCombinedWeaponsScore})");
        Logger.LogDebug($"2H 1 {ideal2H?.Name} ({finalScore2hands}) -- 2H 2 {secondChoice2H?.Name} ({finalScoreSecondChoice2hands})");
        Logger.LogDebug($"1H 1 {idealMainhand?.Name} ({scoreIdealMainHand}) -- 1H 2 {secondChoiceMainhand?.Name} ({scoreSecondChoiceMainHand})");
        Logger.LogDebug($"OFFHAND 1 {idealOffHand?.Name} ({scoreIdealOffhand}) -- OFFHAND 2 {secondChoiceOffhand?.Name} ({scoreSecondOffhand})");
        Logger.LogDebug($"COMBINED 1 {idealMainhand?.Name} + {idealOffHand?.Name} ({finalScoreDualWield}) -- COMBINED 2 {secondChoiceMainhand?.Name} + {secondChoiceOffhand?.Name} ({finalScoreSecondDualWield})");


        if (finalScoreDualWield > currentCombinedWeaponsScore
            || finalScore2hands > currentCombinedWeaponsScore)
        {
            Logger.LogDebug("We've found a Better IDEAL Combination of weapons");
            if (finalScore2hands > finalScoreDualWield)
            {
                ideal2H.Equip(MainHand.InventorySlotID, true);
                return;
            }
            else
            {
                if (idealMainhand != null)
                {
                    idealMainhand?.Equip(MainHand.InventorySlotID, true);
                    idealOffHand?.Equip(OffHand.InventorySlotID, true);
                    return;
                }
            }
        }

        if (finalScoreSecondDualWield > currentCombinedWeaponsScore
            || finalScoreSecondChoice2hands > currentCombinedWeaponsScore)
        {
            Logger.LogDebug("We've found a Better NOT IDEAL combination of weapons");
            if (finalScoreSecondChoice2hands > finalScoreSecondDualWield)
            {
                secondChoice2H.Equip(MainHand.InventorySlotID, true);
                return;
            }
            else
            {
                if (secondChoiceMainhand != null)
                {
                    secondChoiceMainhand?.Equip(MainHand.InventorySlotID, true);
                    secondChoiceOffhand?.Equip(OffHand.InventorySlotID, true);
                    return;
                }
            }
        }
    }

    private static bool WeaponIsIdeal(WAEItem weapon)
    {
        if (weapon == null || !ItemSkillsDictionary.ContainsKey(weapon.ItemSubType))
            return false;

        if (ClassSpec == ClassSpec.RogueAssassination
            && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.Daggers)
            return false;

        if ((ClassSpec == ClassSpec.WarriorArms || KnowTitansGrip)
            && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.TwoHandedSwords
            && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.TwoHandedAxes
            && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.TwoHandedMaces)
            return false;

        if (AutoEquipSettings.CurrentSettings.EquipShields
            && ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.Shield)
            return true;

        if (AutoEquipSettings.CurrentSettings.EquipTwoHanders
            && TwoHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]))
            return true;

        if (AutoEquipSettings.CurrentSettings.EquipOneHanders
            && ItemSkillsDictionary[weapon.ItemSubType] != SkillLine.Shield
            && OneHanders.Contains(ItemSkillsDictionary[weapon.ItemSubType]))
            return true;

        return false;
    }

    private static bool SuitableForTitansGrips(WAEItem weapon)
    {
        return KnowTitansGrip
            && (ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedSwords
            || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedAxes
            || ItemSkillsDictionary[weapon.ItemSubType] == SkillLine.TwoHandedMaces);
    }

    private static List<WAEItem> GetEquipableWeaponsFromBags(WAECharacterSheetSlot slot)
    {
        return WAEContainers.AllItems
            .FindAll(i =>
                i.CanEquip()
                && slot.InvTypes.Contains(i.ItemEquipLoc))
            .ToList();
    }

    public static void CheckSwapWeapons()
    {
        if (MainHand.Item?.ItemEquipLoc == "INVTYPE_WEAPON"
            && OffHand.Item?.ItemEquipLoc == "INVTYPE_WEAPON"
            && MainHand.Item.WeaponSpeed < OffHand.Item.WeaponSpeed)
        {
            Logger.Log("Swapping weapons to have slower speed in main hand");
            MainHand.Item.DropInInventory(MainHand.InventorySlotID);
            MainHand.Item.DropInInventory(OffHand.InventorySlotID);
        }
    }

    public static void AutoEquip()
    {
        //Logger.LogDebug("*** Auto equip...");
        DateTime dateBegin = DateTime.Now;

        AutoEquipAmor();
        AutoEquipRings();
        AutoEquipTrinkets();
        AutoEquipWeapons();
        AutoEquipRanged();
        AutoEquipAmmo();
        CheckSwapWeapons();

        //Logger.LogDebug($"Auto Equip Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    public static void RecordKnownSpecialSkills()
    {
        // TODO Delay
        if (DualWield == null)
            DualWield = new Spell("Dual Wield");

        if (!KnowTitansGrip && ClassSpec == ClassSpec.WarriorFury)
            KnowTitansGrip = ToolBox.GetTalentRank(2, 27) > 0;
    }

    public static void RecordKnownSkills()
    {
        MySkills.Clear();

        string luaListAllSkills = Lua.LuaDoString<string>($@"
                    local result = ""$"";
                    for i = 1, GetNumSkillLines() do
                        local skillName, header, isExpanded, skillRank, numTempPoints, skillModifier,
                            skillMaxRank, isAbandonable, stepCost, rankCost, minLevel, skillCostType,
                            skillDescription = GetSkillLineInfo(i)
                        result = result..skillName..""|""..skillRank..""$""
                    end
                    return result");

        List<string> skills = luaListAllSkills.Split('$').ToList();
        foreach (string skill in skills)
        {
            if (skill.Length > 0)
            {
                string[] skillPair = skill.Split('|');
                string skillName = skillPair[0]
                    .Replace("Shield", "Shields")
                    .Replace("Plate Mail", "Plate");
                if (skillName == "Axes" || skillName == "Swords" || skillName == "Maces")
                    skillName = "One-Handed " + skillName;
                //Logger.Log($"Adding {skillName} to known skills {int.Parse(skillPair[1])}");
                if (!MySkills.ContainsKey(skillName))
                    MySkills.Add(skillName, int.Parse(skillPair[1]));
            }
        }
    }

    public static List<WAECharacterSheetSlot> AllSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Ammo,
        Head,
        Neck,
        Shoulder,
        Back,
        Chest,
        Wrist,
        Hands,
        Waist,
        Legs,
        Feet,
        Finger1,
        Finger2,
        Trinket1,
        Trinket2,
        MainHand,
        OffHand,
        Ranged
    };

    public static List<WAECharacterSheetSlot> ArmorSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Head,
        Neck,
        Shoulder,
        Back,
        Chest,
        Wrist,
        Hands,
        Waist,
        Legs,
        Feet
    };
}