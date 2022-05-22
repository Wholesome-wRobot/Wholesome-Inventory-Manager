using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal interface IWIMContainer
    {
        IWIMItem BagItem { get; }
        int Position { get; }
        int Capacity { get; }
        bool IsAmmoPouch { get; }
        bool IsQuiver { get; }
        bool IsOriginalBackpack { get; }
        SynchronizedCollection<IContainerSlot> Slots { get; }
        SynchronizedCollection<IWIMItem> Items { get; }

        void MoveToSlot(int position);
        int GetContainerNbFreeSlots();
        int GetContainerNbSlots();
        string GetContainerItemlink(int slot);
    }
}
