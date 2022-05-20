using System.Collections.Generic;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal class ItemCache
    {
        private static List<IWIMItem> _itemDb = new List<IWIMItem>();

        public static IWIMItem Get(string itemLink)
        {
            return _itemDb.Find(it => it.ItemLink.Equals(itemLink));
        }

        public static void Add(IWIMItem item)
        {
            if (_itemDb.Count > 300)
            {
                _itemDb.RemoveRange(0, 100);
            }

            if (!ContainsByItemLink(item.Name))
            {
                _itemDb.Add(item);
            }
        }

        private static bool ContainsByItemLink(string itemlink) => _itemDb.Exists(it => it.ItemLink == itemlink);
        public static void ClearCache() => _itemDb.Clear();
    }
}
