using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal class ContainerSlot : IContainerSlot
    {
        public int SlotIndex { get; }
        public int BagPosition { get; }
        public IWIMItem OccupiedBy { get; }

        public ContainerSlot(int slotIndex, int bag, IWIMItem occupiedBy)
        {
            SlotIndex = slotIndex;
            BagPosition = bag;
            OccupiedBy = occupiedBy;
        }
    }
}
