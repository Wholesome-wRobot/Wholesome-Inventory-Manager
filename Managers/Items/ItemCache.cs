using System.Collections.Generic;
using System.Linq;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal class ItemCache
    {
        private static List<IWIMItem> _itemDb = new List<IWIMItem>();
        private static object _cacheLocker = new object();

        public static IWIMItem Get(string itemLink) => _itemDb.FirstOrDefault(it => it.ItemLink.Equals(itemLink));

        public static void Add(IWIMItem item)
        {
            lock(_cacheLocker)
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
        }

        private static bool ContainsByItemLink(string itemlink) => _itemDb.Exists(it => it.ItemLink == itemlink);
        public static void ClearCache()
        {
            lock (_cacheLocker)
            {
                _itemDb.Clear();
            }
        }
    }
}
