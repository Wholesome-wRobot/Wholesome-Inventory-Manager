using System;
using System.Collections.Generic;
using wManager.Wow.ObjectManager;

public class WAELootFilter
{
    public static List<string> ProtectedItems { get; set; } = new List<string>();

    public static void FilterLoot()
    {
        if (!AutoEquipSettings.CurrentSettings.LootFilterActivated)
            return;

        DateTime dateBegin = DateTime.Now;

        int valueThresholdInCopper = AutoEquipSettings.CurrentSettings.DeleteGoldValue * 10000
            + AutoEquipSettings.CurrentSettings.DeleteSilverValue * 100
            + AutoEquipSettings.CurrentSettings.DeleteCopperValue;

        foreach (WAEItem item in WAEContainers.AllItems)
        {
            // Skip Do Not Sell item
            if (wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name) || ProtectedItems.Contains(item.ItemLink))
                continue;

            // Deprecated quest
            if (AutoEquipSettings.CurrentSettings.DeleteDeprecatedQuestItems
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
            if (ToolBox.GetWoWVersion() > ToolBox.WoWVersion.TBC && item.ItemSellPrice == 0 && !AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue)
                continue;

            // Rarity
            if (item.ItemRarity == 0 && AutoEquipSettings.CurrentSettings.DeleteGray)
            {
                item.DeleteFromBag("Quality is Poor");
                continue;
            }
            else if (item.ItemRarity == 0 && AutoEquipSettings.CurrentSettings.KeepGray)
                continue;

            if (item.ItemRarity == 1 && AutoEquipSettings.CurrentSettings.DeleteWhite)
            {
                item.DeleteFromBag("Quality is Common");
                continue;
            }
            else if (item.ItemRarity == 1 && AutoEquipSettings.CurrentSettings.KeepWhite)
                continue;

            if (item.ItemRarity == 2 && AutoEquipSettings.CurrentSettings.DeleteGreen)
            {
                item.DeleteFromBag("Quality is Uncommon");
                continue;
            }
            else if (item.ItemRarity == 2 && AutoEquipSettings.CurrentSettings.KeepGreen)
                continue;

            if (item.ItemRarity == 3 && AutoEquipSettings.CurrentSettings.DeleteBlue)
            {
                item.DeleteFromBag("Quality is rare");
                continue;
            }
            else if (item.ItemRarity == 3 && AutoEquipSettings.CurrentSettings.KeepBlue)
                continue;

            if (item.ItemSellPrice < valueThresholdInCopper)
                item.DeleteFromBag($"Item value {item.ItemSellPrice} is lesser than setting {valueThresholdInCopper}");
        }

        Logger.LogPerformance($"Loot Filter Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    public static void ProtectFromFilter(string itemLink)
    {
        if (!ProtectedItems.Contains(itemLink))
            ProtectedItems.Add(itemLink);
    }

    public static void AllowForFilter(string itemLink)
    {
        if (ProtectedItems.Contains(itemLink))
            ProtectedItems.Remove(itemLink);
    }
}
