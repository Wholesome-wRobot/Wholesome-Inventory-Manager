using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;

namespace Wholesome_Inventory_Manager.Managers.Filter
{
    internal interface ILootFilter : ICycleable
    {
        void ProtectFromFilter(string itemLink);
        void AllowForFilter(string itemLink);
        void FilterLoot(SynchronizedCollection<IWIMItem> bagItems);
    }
}
