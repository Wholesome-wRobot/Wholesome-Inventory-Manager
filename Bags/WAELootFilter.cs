using System.Collections.Generic;

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
            //Logger.Log($"{item.Name} - {item.ItemRarity} - {item.ItemSellPrice} - {item.ItemSubType}");

            // Rarity
            if (item.ItemRarity == 0 && AutoEquipSettings.CurrentSettings.DeleteGray)
                item.DeleteFromBag("Quality is Poor");
            else if (item.ItemRarity == 0 && AutoEquipSettings.CurrentSettings.KeepGray)
                continue;

            if (item.ItemRarity == 1 && AutoEquipSettings.CurrentSettings.DeleteWhite)
                item.DeleteFromBag("Quality is Common");
            else if (item.ItemRarity == 1 && AutoEquipSettings.CurrentSettings.KeepWhite)
                continue;

            if (item.ItemRarity == 2 && AutoEquipSettings.CurrentSettings.DeleteGreen)
                item.DeleteFromBag("Quality is Uncommon");
            else if (item.ItemRarity == 2 && AutoEquipSettings.CurrentSettings.KeepGreen)
                continue;

            if (item.ItemRarity == 3 && AutoEquipSettings.CurrentSettings.DeleteBlue)
                item.DeleteFromBag("Quality is Rare");
            else if (item.ItemRarity == 3 && AutoEquipSettings.CurrentSettings.KeepBlue)
                continue;

            // VALUE
            if (item.ItemSellPrice == 0 && !AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue)
                continue;

            if (item.ItemSellPrice <= valueThresholdInCopper)
                item.DeleteFromBag($"Item value {item.ItemSellPrice} is lesser than setting {valueThresholdInCopper}");

        }
    }
}
