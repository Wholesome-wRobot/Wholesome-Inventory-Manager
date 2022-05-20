using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal interface IWIMContainers : ICycleable
    {
        bool HaveBulletsInBags { get; }
        bool HaveArrowsInBags { get; }
        List<IWIMItem> GetAllBagItems();
        void Scan();
        void BagEquip();
    }
}
