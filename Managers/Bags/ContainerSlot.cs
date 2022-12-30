using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal class ContainerSlot : IContainerSlot
    {
        public int SlotIndex { get; }
        public int BagIndex { get; }
        public IWIMItem OccupiedBy { get; }

        public ContainerSlot(int slotIndex, int bagIndex, IWIMItem occupiedBy)
        {
            SlotIndex = slotIndex;
            BagIndex = bagIndex;
            OccupiedBy = occupiedBy;
        }
    }
}
