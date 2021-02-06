public class WAECharacterSheetSlot
{
    public WAEItem Item { get; set; }
    public int InventorySlotID { get; set; }

    public WAECharacterSheetSlot(int inventorySlotID)
    {
        InventorySlotID = inventorySlotID;
        string itemLink = WAECharacterSheet.AllItemLinks[InventorySlotID];
        if (itemLink != "null")
        {
            Item = new WAEItem(itemLink);
        }
    }
}
