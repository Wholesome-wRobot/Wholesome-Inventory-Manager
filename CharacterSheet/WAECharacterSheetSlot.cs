public class WAECharacterSheetSlot
{
    public WAEItem Item { get; set; }
    public int InventorySlotID { get; set; }
    public string[] InvTypes { get; set; }

    public WAECharacterSheetSlot(int inventorySlotID, string[] invTypes)
    {
        InvTypes = invTypes;
        InventorySlotID = inventorySlotID;
    }

    public void RefreshItem()
    {
        string itemLink = WAECharacterSheet.AllItemLinks[InventorySlotID];
        Item = itemLink != "null" ? new WAEItem(itemLink) : null;
    }
}
