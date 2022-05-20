using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal interface ISheetSlot
    {
        IWIMItem Item { get; }
        string[] InvTypes { get; }
        int InventorySlotID { get; }

        void RefreshItem(string itemLink);
    }
}
