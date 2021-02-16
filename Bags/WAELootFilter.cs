using System.Collections.Generic;
using wManager.Wow.ObjectManager;

public class WAELootFilter
{
    public static void FilterLoot()
    {
        if (!AutoEquipSettings.CurrentSettings.LootFilterActivated)
            return;

        List<WAEItem> itemsToDelete = new List<WAEItem>();

        int valueThresholdInCopper = AutoEquipSettings.CurrentSettings.DeleteGoldValue * 10000
            + AutoEquipSettings.CurrentSettings.DeleteSilverValue * 100
            + AutoEquipSettings.CurrentSettings.DeleteCopperValue;

        foreach (WAEItem item in WAEContainers.AllItems)
        {
            //Logger.Log($"{item.Name} - {item.ItemRarity} - {item.ItemType} - {item.ItemSubType} - {item.ItemMinLevel}");

            // Deprecated quest
            if (AutoEquipSettings.CurrentSettings.DeleteDeprecatedQuestItems
                && item.ItemMinLevel > 0
                && item.ItemType == "Quest" 
                && item.ItemSubType == "Quest"
                && ObjectManager.Me.Level > item.ItemMinLevel + 4)
            {
                item.DeleteFromBag("Quest is deprecated");
                continue;
            }

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

            // Value
            if (item.ItemSellPrice == 0 && !AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue)
                continue;

            if (item.ItemSellPrice <= valueThresholdInCopper)
                item.DeleteFromBag($"Item value {item.ItemSellPrice} is lesser than setting {valueThresholdInCopper}");

        }
    }
}
