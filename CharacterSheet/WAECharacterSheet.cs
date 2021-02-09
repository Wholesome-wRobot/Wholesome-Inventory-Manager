using System;
using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public static class WAECharacterSheet
{
    public static WAECharacterSheetSlot Ammo { get; set; } = new WAECharacterSheetSlot(0, new string[] { "INVTYPE_AMMO" });
    public static WAECharacterSheetSlot Head { get; set; } = new WAECharacterSheetSlot(1, new string[] { "INVTYPE_HEAD" });
    public static WAECharacterSheetSlot Neck { get; set; } = new WAECharacterSheetSlot(2, new string[] { "INVTYPE_NECK" });
    public static WAECharacterSheetSlot Shoulder { get; set; } = new WAECharacterSheetSlot(3, new string[] { "INVTYPE_SHOULDER" });
    public static WAECharacterSheetSlot Chest { get; set; } = new WAECharacterSheetSlot(5, new string[] { "INVTYPE_CHEST" });
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
    public static WAECharacterSheetSlot MainHand { get; set; } = new WAECharacterSheetSlot(16, new string[] { "INVTYPE_WEAPON", "INVTYPE_WEAPONMAINHAND",  "INVTYPE_2HWEAPON" });
    public static WAECharacterSheetSlot OffHand { get; set; } = new WAECharacterSheetSlot(17, new string[] { "INVTYPE_WEAPON", "INVTYPE_SHIELD", "INVTYPE_HOLDABLE", "INVTYPE_WEAPONOFFHAND", });
    public static WAECharacterSheetSlot Ranged { get; set; } = new WAECharacterSheetSlot(18, new string[] { "INVTYPE_RANGEDRIGHT", "INVTYPE_RANGED", "INVTYPE_THROWN" });
    public static List<string> AllItemLinks { get; set; } = new List<string>();
    public static List<SkillLine> MySkills { get; set; } = new List<SkillLine>();

    public static void Scan()
    {
        Logger.LogDebug("*** Scanning character sheet...");
        DateTime dateBegin = DateTime.Now;

        RecordKnownSkills();

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

        Logger.LogDebug($"CharSheet Scan Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    public static void AutoEquip()
    {
        Logger.LogDebug("*** Auto equip...");
        DateTime dateBegin = DateTime.Now;

        // ----------------- Auto equip armor -----------------
        foreach (WAECharacterSheetSlot armorSlot in ArmorSlots)
        {
            Logger.LogDebug($"{armorSlot.InventorySlotID} -> {armorSlot.InvTypes} -> {armorSlot.Item?.Name} ({armorSlot.Item?.WeightScore})");
            // List potential replacement for this slot
            List<WAEItem> potentialArmors = WAEBagInventory.AllItems
                .FindAll(i => 
                    armorSlot.InvTypes.Contains(i.ItemEquipLoc)
                    && i.CanEquip() 
                    && i.GetNbEquipAttempts() < 3)
                .OrderByDescending(i => i.WeightScore)
                .ToList();

            foreach(WAEItem item in potentialArmors)
            {
                Logger.LogDebug($"Potential item: {item.Name} ({item.WeightScore})");
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

        // ----------------- Auto Equip Rings -----------------
        float ring1Score = Finger1.Item != null ? Finger1.Item.WeightScore : 0;
        float ring2Score = Finger2.Item != null ? Finger2.Item.WeightScore : 0;
        WAECharacterSheetSlot lowestScoreFingerSlot = ring1Score <= ring2Score ? Finger1 : Finger2;

        // List potential replacement for this slot
        List<WAEItem> potentialRings = WAEBagInventory.AllItems
            .FindAll(i =>
                i.ItemEquipLoc == "INVTYPE_FINGER"
                && i.CanEquip()
                && i.GetNbEquipAttempts() < 3)
            .OrderByDescending(i => i.WeightScore)
            .ToList();
        
        foreach (WAEItem item in potentialRings)
        {
            Logger.LogDebug($"Potential Ring: {item.Name} ({item.WeightScore})");
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

        // ----------------- Auto Equip Trinkets -----------------
        float trinket1Score = Trinket1.Item != null ? Trinket1.Item.WeightScore : 0;
        float trinket2Score = Trinket2.Item != null ? Trinket2.Item.WeightScore : 0;
        WAECharacterSheetSlot lowestScoreTrinketSlot = trinket1Score <= trinket2Score ? Trinket1 : Trinket2;

        // List potential replacement for this slot
        List<WAEItem> potentialTrinkets = WAEBagInventory.AllItems
            .FindAll(i =>
                i.ItemEquipLoc == "INVTYPE_TRINKET"
                && i.CanEquip()
                && i.GetNbEquipAttempts() < 3)
            .OrderByDescending(i => i.WeightScore)
            .ToList();

        foreach (WAEItem item in potentialTrinkets)
        {
            Logger.LogDebug($"Potential Trinket: {item.Name} ({item.WeightScore})");
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

        // ----------------- Auto Equip Weapons -----------------

        // ----------------- Auto Equip Ranged -----------------
        bool haveBulletsInBags = WAEBagInventory.AllItems.Exists(i => i.ItemSubType == "Bullet" && ObjectManager.Me.Level >= i.ItemMinLevel);
        bool haveArrowsInBags = WAEBagInventory.AllItems.Exists(i => i.ItemSubType == "Arrow" && ObjectManager.Me.Level >= i.ItemMinLevel);

        // List potential replacement for this slot
        List<WAEItem> potentialRanged = WAEBagInventory.AllItems
            .FindAll(i =>
                Ranged.InvTypes.Contains(i.ItemEquipLoc)
                && i.CanEquip()
                && i.GetNbEquipAttempts() < 3)
            .OrderByDescending(i => i.WeightScore)
            .ToList();

        foreach (WAEItem item in potentialRanged)
        {
            if (item.ItemSubType == "Guns" && !haveBulletsInBags)
                continue;
            if ((item.ItemSubType == "Crossbows" || item.ItemSubType == "Bows") && !haveArrowsInBags)
                continue;

            Logger.LogDebug($"Potential Ranged: {item.Name} ({item.ItemMinLevel})");

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

            if (Ranged.Item == null
                || Ranged.Item.WeightScore < item.WeightScore
                || (Ranged.Item.ItemSubType == "Crossbows" && !haveArrowsInBags)
                || (Ranged.Item.ItemSubType == "Bows" && !haveArrowsInBags)
                || (Ranged.Item.ItemSubType == "Guns" && !haveBulletsInBags))
            {
                if (itemTypeIsBanned)
                    Logger.Log($"Equipping {item.Name} ({item.WeightScore}) until we find a better option");
                else if (Ranged.Item == null)
                    Logger.Log($"Equipping {item.Name} ({item.WeightScore})");
                else
                    Logger.Log($"Replacing {Ranged.Item.Name} ({Ranged.Item.WeightScore}) with {item.Name} ({item.WeightScore})");

                if (item.Equip(Ranged.InventorySlotID))
                    break;
            }
        }


        // ----------------- Auto Equip Ammo -----------------
        if (Ranged.Item != null)
        {
            string typeAmmo = null;
            if (Ranged.Item.ItemSubType == "Crossbows" || Ranged.Item.ItemSubType == "Bows")
                typeAmmo = "Arrow";
            if (Ranged.Item.ItemSubType == "Guns")
                typeAmmo = "Bullet";

            // List potential replacement for this slot
            List<WAEItem> potentialAmmo = WAEBagInventory.AllItems
                .FindAll(i =>
                    i.ItemSubType == typeAmmo
                    && ObjectManager.Me.Level >= Ranged.Item.ItemMinLevel
                    && i.GetNbEquipAttempts() < 3)
                .OrderByDescending(i => i.ItemMinLevel)
                .ToList();
            
            foreach (WAEItem item in potentialAmmo)
            {
                Logger.LogDebug($"Potential ammo: {item.Name} ({item.ItemMinLevel})");

                if (Ammo.Item == null 
                    || Ammo.Item.ItemMinLevel < item.ItemMinLevel
                    || Ammo.Item.ItemSubType != item.ItemSubType
                    || !WAEBagInventory.AllItems.Exists(i => i.ItemId == item.ItemId))
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

        Logger.LogDebug($"Auto Equip Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    public static void RecordKnownSkills()
    {
        foreach (KeyValuePair<string, SkillLine> skill in ItemSkillsDictionary)
        {
            if (Skill.Has(skill.Value) && !MySkills.Contains(skill.Value))
                MySkills.Add(skill.Value);
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

    public static Dictionary<string, SkillLine> ItemSkillsDictionary = new Dictionary<string, SkillLine>
    {
        { "Swords", SkillLine.Swords },
        { "TwoHandedSwords", SkillLine.TwoHandedSwords },
        { "Axes", SkillLine.Axes },
        { "TwoHandedAxes", SkillLine.TwoHandedAxes },
        { "TwoHandedMaces", SkillLine.TwoHandedMaces },
        { "Bows", SkillLine.Bows },
        { "Guns", SkillLine.Guns },
        { "Crossbows", SkillLine.Crossbows },
        { "Wands", SkillLine.Wands },
        { "Daggers", SkillLine.Daggers },
        { "FistWeapons", SkillLine.FistWeapons },
        { "Staves", SkillLine.Staves },
        { "Polearms", SkillLine.Polearms },
        { "Thrown", SkillLine.Thrown },
        { "Cloth", SkillLine.Cloth },
        { "Leather", SkillLine.Leather },
        { "Mail", SkillLine.Mail },
        { "Plate", SkillLine.PlateMail },
        { "Shield", SkillLine.Shield }
    };
}