using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal interface IContainerSlot
    {
        int SlotIndex { get; }
        int BagIndex { get; }
        IWIMItem OccupiedBy { get; }
    }
}
