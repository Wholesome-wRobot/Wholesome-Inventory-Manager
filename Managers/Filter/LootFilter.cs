using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.ObjectManager;

namespace Wholesome_Inventory_Manager.Managers.Filter
{
    internal class LootFilter : ILootFilter
    {
        private SynchronizedCollection<string> _protectedItems = new SynchronizedCollection<string>();
        private object _filterLock = new object();
        private readonly bool _versionIsHigherThanTBC = ToolBox.GetWoWVersion() > ToolBox.WoWVersion.TBC;
        private readonly bool _deleteDeprecatedQuestItems;
        private readonly int _deleteGoldValue;
        private readonly int _deleteSilverValue;
        private readonly int _deleteCopperValue;
        private readonly int _valueThresholdInCopper;
        private readonly bool _deleteItemWithNoValue;
        private readonly bool _deleteGray;
        private readonly bool _deleteWhite;
        private readonly bool _deleteGreen;
        private readonly bool _deleteBlue;
        private readonly bool _keepGray;
        private readonly bool _keepWhite;
        private readonly bool _keepGreen;
        private readonly bool _keepBlue;
        private readonly bool _lootFilterActivated;

        public LootFilter()
        {
            _deleteDeprecatedQuestItems = AutoEquipSettings.CurrentSettings.DeleteDeprecatedQuestItems;
            _deleteGoldValue = AutoEquipSettings.CurrentSettings.DeleteGoldValue;
            _deleteSilverValue = AutoEquipSettings.CurrentSettings.DeleteSilverValue;
            _deleteCopperValue = AutoEquipSettings.CurrentSettings.DeleteCopperValue;
            _valueThresholdInCopper = _deleteGoldValue * 10000 + _deleteSilverValue * 100 + _deleteCopperValue;
            _deleteItemWithNoValue = AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue;
            _deleteGray = AutoEquipSettings.CurrentSettings.DeleteGray;
            _deleteWhite = AutoEquipSettings.CurrentSettings.DeleteWhite;
            _deleteGreen = AutoEquipSettings.CurrentSettings.DeleteGreen;
            _deleteBlue = AutoEquipSettings.CurrentSettings.DeleteBlue;
            _keepGray = AutoEquipSettings.CurrentSettings.KeepGray;
            _keepWhite = AutoEquipSettings.CurrentSettings.KeepWhite;
            _keepGreen = AutoEquipSettings.CurrentSettings.KeepGreen;
            _keepBlue = AutoEquipSettings.CurrentSettings.KeepBlue;
            _lootFilterActivated = AutoEquipSettings.CurrentSettings.LootFilterActivated;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        public void FilterLoot(SynchronizedCollection<IWIMItem> bagItems)
        {
            if (!_lootFilterActivated) return;

            lock (_filterLock)
            {
                foreach (IWIMItem item in bagItems)
                {
                    // Skip Do Not Sell item
                    if (wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name) || _protectedItems.Contains(item.ItemLink))
                        continue;

                    // Deprecated quest
                    if (_deleteDeprecatedQuestItems
                        && item.ItemMinLevel > 1
                        && item.ItemType == "Quest"
                        && item.ItemSubType == "Quest"
                        && ObjectManager.Me.Level > item.ItemMinLevel + 6)
                    {
                        item.DeleteFromBag($"Quest level {item.ItemMinLevel} is deprecated");
                        continue;
                    }

                    // Skip quest
                    if (item.ItemType == "Quest"
                        || item.ItemSubType == "Quest"
                        || item.ItemType == "Key"
                        || item.ItemSubType == "Key")
                        continue;

                    // Value
                    if (_versionIsHigherThanTBC && item.ItemSellPrice == 0 && !_deleteItemWithNoValue)
                        continue;

                    // Rarity
                    if (item.ItemRarity == 0 && _deleteGray)
                    {
                        item.DeleteFromBag("Quality is Poor");
                        continue;
                    }
                    else if (item.ItemRarity == 0 && _keepGray)
                        continue;

                    if (item.ItemRarity == 1 && _deleteWhite)
                    {
                        item.DeleteFromBag("Quality is Common");
                        continue;
                    }
                    else if (item.ItemRarity == 1 && _keepWhite)
                        continue;

                    if (item.ItemRarity == 2 && _deleteGreen)
                    {
                        item.DeleteFromBag("Quality is Uncommon");
                        continue;
                    }
                    else if (item.ItemRarity == 2 && _keepGreen)
                        continue;

                    if (item.ItemRarity == 3 && _deleteBlue)
                    {
                        item.DeleteFromBag("Quality is rare");
                        continue;
                    }
                    else if (item.ItemRarity == 3 && _keepBlue)
                        continue;

                    if (item.ItemSellPrice < _valueThresholdInCopper)
                        item.DeleteFromBag($"Item value {item.ItemSellPrice} is lesser than setting {_valueThresholdInCopper}");
                }
            }
        }

        public void ProtectFromFilter(string itemLink)
        {
            if (!_protectedItems.Contains(itemLink))
            {
                _protectedItems.Add(itemLink);
            }
        }

        public void AllowForFilter(string itemLink)
        {
            if (_protectedItems.Contains(itemLink))
            {
                _protectedItems.Remove(itemLink);
            }
        }
    }
}
