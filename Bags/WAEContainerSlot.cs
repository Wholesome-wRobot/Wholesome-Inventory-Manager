public class WAEContainerSlot
{
    public int Slot { get; set; }
    public int BagPosition { get; set; }
    public WAEItem OccupiedBy { get; set; }

    public WAEContainerSlot(int slot, int bag, WAEItem occupiedBy)
    {
        Slot = slot;
        BagPosition = bag;
        OccupiedBy = occupiedBy;
    }
}