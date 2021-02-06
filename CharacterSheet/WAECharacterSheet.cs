using System;
using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Helpers;

public static class WAECharacterSheet
{
    public static WAECharacterSheetSlot Ammo { get; set; }
    public static WAECharacterSheetSlot Head { get; set; }
    public static WAECharacterSheetSlot Neck { get; set; }
    public static WAECharacterSheetSlot Shoulder { get; set; }
    public static WAECharacterSheetSlot Back { get; set; }
    public static WAECharacterSheetSlot Chest { get; set; }
    public static WAECharacterSheetSlot Shirt { get; set; }
    public static WAECharacterSheetSlot Tabard { get; set; }
    public static WAECharacterSheetSlot Wrist { get; set; }
    public static WAECharacterSheetSlot Hands { get; set; }
    public static WAECharacterSheetSlot Waist { get; set; }
    public static WAECharacterSheetSlot Legs { get; set; }
    public static WAECharacterSheetSlot Feet { get; set; }
    public static WAECharacterSheetSlot Finger1 { get; set; }
    public static WAECharacterSheetSlot Finger2 { get; set; }
    public static WAECharacterSheetSlot Trinket1 { get; set; }
    public static WAECharacterSheetSlot Trinket2 { get; set; }
    public static WAECharacterSheetSlot MainHand { get; set; }
    public static WAECharacterSheetSlot OffHand { get; set; }
    public static WAECharacterSheetSlot Ranged { get; set; }
    public static List<string> AllItemLinks { get; set; } = new List<string>();

    public static void Scan()
    {
        Logger.Log("Scanning character sheet...");
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

        Ammo = new WAECharacterSheetSlot(0);
        Head = new WAECharacterSheetSlot(1);
        Neck = new WAECharacterSheetSlot(2);
        Shoulder = new WAECharacterSheetSlot(3);
        Shirt = new WAECharacterSheetSlot(4);
        Chest = new WAECharacterSheetSlot(5);
        Waist = new WAECharacterSheetSlot(6);
        Legs = new WAECharacterSheetSlot(7);
        Feet = new WAECharacterSheetSlot(8);
        Wrist = new WAECharacterSheetSlot(9);
        Hands = new WAECharacterSheetSlot(10);
        Finger1 = new WAECharacterSheetSlot(11);
        Finger2 = new WAECharacterSheetSlot(12);
        Trinket1 = new WAECharacterSheetSlot(13);
        Trinket2 = new WAECharacterSheetSlot(14);
        Back = new WAECharacterSheetSlot(15);
        MainHand = new WAECharacterSheetSlot(16);
        OffHand = new WAECharacterSheetSlot(17);
        Ranged = new WAECharacterSheetSlot(18);
        Tabard = new WAECharacterSheetSlot(19);

        Logger.Log($"CharSheet Scan Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }
}