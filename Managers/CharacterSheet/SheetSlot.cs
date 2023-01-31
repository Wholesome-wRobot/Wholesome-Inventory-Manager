using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal class SheetSlot : ISheetSlot
    {
        public IWIMItem Item { get; private set; }
        public int InventorySlotID { get; }
        public string[] InvTypes { get; }

        public SheetSlot(int inventorySlotID, string[] invTypes)
        {
            InventorySlotID = inventorySlotID;
            InvTypes = invTypes;
        }

        public void RefreshItem(string itemLink)
        {
            Item = itemLink != "null" ? new WIMItem(itemLink) : null;
        }

        public string GetItemLink => Item == null ? null : Item.ItemLink;
    }
}
