using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal class CharacterSheetManager : ICharacterSheetManager
    {
        private ISheetSlot Ammo { get; } = new SheetSlot(0, new string[] { "INVTYPE_AMMO" });
        private ISheetSlot Head { get; } = new SheetSlot(1, new string[] { "INVTYPE_HEAD" });
        private ISheetSlot Neck { get; } = new SheetSlot(2, new string[] { "INVTYPE_NECK" });
        private ISheetSlot Shoulder { get; } = new SheetSlot(3, new string[] { "INVTYPE_SHOULDER" });
        private ISheetSlot Chest { get; } = new SheetSlot(5, new string[] { "INVTYPE_CHEST", "INVTYPE_ROBE" });
        private ISheetSlot Waist { get; } = new SheetSlot(6, new string[] { "INVTYPE_WAIST" });
        private ISheetSlot Legs { get; } = new SheetSlot(7, new string[] { "INVTYPE_LEGS" });
        private ISheetSlot Feet { get; } = new SheetSlot(8, new string[] { "INVTYPE_FEET" });
        private ISheetSlot Wrist { get; } = new SheetSlot(9, new string[] { "INVTYPE_WRIST" });
        private ISheetSlot Hands { get; } = new SheetSlot(10, new string[] { "INVTYPE_HAND" });
        private ISheetSlot Finger1 { get; } = new SheetSlot(11, new string[] { "INVTYPE_FINGER" });
        private ISheetSlot Finger2 { get; } = new SheetSlot(12, new string[] { "INVTYPE_FINGER" });
        private ISheetSlot Trinket1 { get; } = new SheetSlot(13, new string[] { "INVTYPE_TRINKET" });
        private ISheetSlot Trinket2 { get; } = new SheetSlot(14, new string[] { "INVTYPE_TRINKET" });
        private ISheetSlot Back { get; } = new SheetSlot(15, new string[] { "INVTYPE_CLOAK" });
        private ISheetSlot MainHand { get; } = new SheetSlot(16, new string[] { "INVTYPE_WEAPON", "INVTYPE_WEAPONMAINHAND", "INVTYPE_2HWEAPON" });
        private ISheetSlot OffHand { get; } = new SheetSlot(17, new string[] { "INVTYPE_WEAPON", "INVTYPE_SHIELD", "INVTYPE_HOLDABLE", "INVTYPE_WEAPONOFFHAND" });
        private ISheetSlot Ranged { get; } = new SheetSlot(18, new string[] { "INVTYPE_RANGEDRIGHT", "INVTYPE_RANGED", "INVTYPE_THROWN" });

        public void Initialize()
        {
            Scan();
        }

        public void Dispose()
        {
        }

        public void Scan()
        {
            List<string> allItemLinks = Lua.LuaDoString<string>($@"
                                local allItems = """";
                                for i=0, 19 do
                                    local item = GetInventoryItemLink(""player"", i);
                                    if item == nil then item = ""null"" end;
                                    allItems = allItems..'$'..item;
                                end
                                return allItems;").Split('$').ToList();
            allItemLinks.RemoveAt(0);

            for (int i = 0; i < allItemLinks.Count; i++)
            {
                ISheetSlot slotToRefresh = AllSlots.Find(s => s.InventorySlotID == i);
                if (slotToRefresh != null)
                {
                    slotToRefresh.RefreshItem(allItemLinks[i]);
                }
            }
        }

        private List<ISheetSlot> AllSlots => new List<ISheetSlot>()
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

        public List<ISheetSlot> ArmorSlots => new List<ISheetSlot>()
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

        public List<ISheetSlot> FingerSlots => new List<ISheetSlot>()
        {
            Finger1,
            Finger2
        };

        public List<ISheetSlot> TrinketSlots => new List<ISheetSlot>()
        {
            Trinket1,
            Trinket2
        };

        public List<ISheetSlot> WeaponSlots => new List<ISheetSlot>()
        {
            MainHand,
            OffHand
        };

        public ISheetSlot RangedSlot => Ranged;
        public ISheetSlot AmmoSlot => Ammo;
    }
}
