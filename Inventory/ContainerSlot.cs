public class ContainerSlot
{
    public int Slot { get; set; }
    public int BagPosition { get; set; }
    public Item OccupiedBy { get; set; }

    public ContainerSlot(int slot, int bag, Item occupiedBy)
    {
        Slot = slot;
        BagPosition = bag;
        OccupiedBy = occupiedBy;
    }
}
