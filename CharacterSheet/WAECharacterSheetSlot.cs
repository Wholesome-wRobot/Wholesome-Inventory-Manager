public class WAECharacterSheetSlot
{
    public WAEItem Item { get; set; }
    public int InventorySlotID { get; set; }
    public string InvType { get; set; }

    public WAECharacterSheetSlot(int inventorySlotID, string invType)
    {
        InvType = invType;
        InventorySlotID = inventorySlotID;
    }

    public void RefreshItem()
    {
        string itemLink = WAECharacterSheet.AllItemLinks[InventorySlotID];
        Item = itemLink != "null" ? new WAEItem(itemLink) : null;
    }
}
