using System;
using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Helpers;

public static class WAECharacterSheet
{
    public static WAECharacterSheetSlot Ammo { get; set; } = new WAECharacterSheetSlot(0, "INVTYPE_AMMO");
    public static WAECharacterSheetSlot Head { get; set; } = new WAECharacterSheetSlot(1, "INVTYPE_HEAD");
    public static WAECharacterSheetSlot Neck { get; set; } = new WAECharacterSheetSlot(2, "INVTYPE_NECK");
    public static WAECharacterSheetSlot Shoulder { get; set; } = new WAECharacterSheetSlot(3, "INVTYPE_SHOULDER");
    public static WAECharacterSheetSlot Chest { get; set; } = new WAECharacterSheetSlot(5, "INVTYPE_CHEST");
    public static WAECharacterSheetSlot Waist { get; set; } = new WAECharacterSheetSlot(6, "INVTYPE_WAIST");
    public static WAECharacterSheetSlot Legs { get; set; } = new WAECharacterSheetSlot(7, "INVTYPE_LEGS");
    public static WAECharacterSheetSlot Feet { get; set; } = new WAECharacterSheetSlot(8, "INVTYPE_FEET");
    public static WAECharacterSheetSlot Wrist { get; set; } = new WAECharacterSheetSlot(9, "INVTYPE_WRIST");
    public static WAECharacterSheetSlot Hands { get; set; } = new WAECharacterSheetSlot(10, "INVTYPE_HAND");
    public static WAECharacterSheetSlot Finger1 { get; set; } = new WAECharacterSheetSlot(11, "INVTYPE_FINGER");
    public static WAECharacterSheetSlot Finger2 { get; set; } = new WAECharacterSheetSlot(12, "INVTYPE_FINGER");
    public static WAECharacterSheetSlot Trinket1 { get; set; } = new WAECharacterSheetSlot(13, "INVTYPE_TRINKET");
    public static WAECharacterSheetSlot Trinket2 { get; set; } = new WAECharacterSheetSlot(14, "INVTYPE_TRINKET");
    public static WAECharacterSheetSlot Back { get; set; } = new WAECharacterSheetSlot(15, "INVTYPE_CLOAK");
    public static WAECharacterSheetSlot MainHand { get; set; } = new WAECharacterSheetSlot(16, "INVTYPE_WEAPON");
    public static WAECharacterSheetSlot OffHand { get; set; } = new WAECharacterSheetSlot(17, "INVTYPE_WEAPON");
    public static WAECharacterSheetSlot Ranged { get; set; } = new WAECharacterSheetSlot(18, "INVTYPE_RANGEDRIGHT");
    public static List<string> AllItemLinks { get; set; } = new List<string>();
    public static List<WAECharacterSheetSlot> AllSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Ammo, Head, Neck, Shoulder, Back, Chest, Wrist, Hands, Waist, Legs, Feet, Finger1, Finger2, Trinket1, Trinket2, MainHand, OffHand, Ranged
    };
    public static List<WAECharacterSheetSlot> ArmorSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Head, Neck, Shoulder, Back, Chest, Wrist, Hands, Waist, Legs, Feet
    };
    public static List<WAECharacterSheetSlot> RingSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Finger1, Finger2
    };
    public static List<WAECharacterSheetSlot> TrinketSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        Trinket1, Trinket2
    };
    public static List<WAECharacterSheetSlot> WeaponSlots { get; set; } = new List<WAECharacterSheetSlot>()
    {
        MainHand, OffHand
    };

    public static void Scan()
    {
        Logger.LogDebug("*** Scanning character sheet...");
        DateTime dateBegin = DateTime.Now;
        
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

        // Auto equip armor
        foreach (WAECharacterSheetSlot armorSlot in ArmorSlots)
        {
            Logger.LogDebug($"{armorSlot.InventorySlotID} -> {armorSlot.InvType} -> {armorSlot.Item?.Name} ({armorSlot.Item?.WeightScore})");
            // List potential replacement for this slot
            List<WAEItem> potentialItems = WAEBagInventory.AllItems
                .FindAll(i => 
                    i.ItemEquipLoc == armorSlot.InvType 
                    && i.CanEquip() 
                    && i.GetNbEquipAttempts() < 3)
                .OrderByDescending(i => i.WeightScore)
                .ToList();

            foreach(WAEItem item in potentialItems)
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

        Logger.LogDebug($"Auto Equip Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

}