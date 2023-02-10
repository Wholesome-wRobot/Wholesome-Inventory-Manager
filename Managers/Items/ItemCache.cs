using System.Collections.Generic;
using System.Linq;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal class ItemCache
    {
        private static List<IWIMItem> _itemDb = new List<IWIMItem>();
        private static object _cacheLocker = new object();

        private static readonly List<uint> StrengthScrolls = new List<uint>() { 954, 2289, 4426, 10310, 27503, 33462, 43465, 43466 };
        private static readonly List<uint> AgilityScrolls = new List<uint>() { 3012, 1477, 4425, 10309, 27498, 33457, 43465, 43464 };
        private static readonly List<uint> IntellectScrolls = new List<uint>() { 955, 2290, 4419, 10308, 27499, 33458, 37091, 37092 };
        private static readonly List<uint> StaminaScrolls = new List<uint>() { 1180, 1711, 4422, 10307, 27502, 33461, 37093, 37094 };
        private static readonly List<uint> SpiritScrolls = new List<uint>() { 1181, 1712, 4424, 10306, 27501, 33460, 37097, 37098 };
        private static readonly List<uint> ProtectionScrolls = new List<uint>() { 3013, 1478, 4421, 10305, 27500, 33459, 43467, 43468 };

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

        public static string GetScrollSpell(uint itemId)
        {
            if (StrengthScrolls.Contains(itemId))
            {
                return "Strength";
            }
            if (AgilityScrolls.Contains(itemId))
            {
                return "Agility";
            }
            if (IntellectScrolls.Contains(itemId))
            {
                return "Intellect";
            }
            if (StaminaScrolls.Contains(itemId))
            {
                return "Stamina";
            }
            if (SpiritScrolls.Contains(itemId))
            {
                return "Spirit";
            }
            if (ProtectionScrolls.Contains(itemId))
            {
                return "Protection";
            }

            return null;
        }
    }
}
