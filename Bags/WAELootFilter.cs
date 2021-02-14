using System.Collections.Generic;

public class WAELootFilter
{
    public static void FilterLoot()
    {
        if (!AutoEquipSettings.CurrentSettings.LootFilterActivated)
            return;

        Logger.Log("FILTER LOOT");
        List<WAEItem> itemsToDelete = new List<WAEItem>();

        foreach (WAEItem item in WAEContainers.AllItems)
        {
            Logger.Log($"{item.Name} - {item.ItemRarity} - {item.ItemSellPrice} - {item.ItemSubType}");

            if (RarityShouldBeDeleted(item))
            {
                Logger.Log($"Deleting {item} because its rarity matches your settings");
                continue;
            }
        }
    }

    private static bool GoldValueShouldBeDeleted(WAEItem item)
    {
        int valueThreshold = AutoEquipSettings.CurrentSettings.DeleteGoldValue * 10000
            + AutoEquipSettings.CurrentSettings.DeleteSilverValue * 100
            + AutoEquipSettings.CurrentSettings.DeleteCopperValue;

        if (item.ItemSellPrice == 0)
            return AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue;

        return item.ItemSellPrice <= valueThreshold;
    }

    private static bool RarityShouldBeDeleted(WAEItem item)
    {
        if (item.ItemRarity == 0 && AutoEquipSettings.CurrentSettings.DeleteGray) return true;
        if (item.ItemRarity == 1 && AutoEquipSettings.CurrentSettings.DeleteWhite) return true;
        if (item.ItemRarity == 2 && AutoEquipSettings.CurrentSettings.DeleteGreen) return true;
        if (item.ItemRarity == 3 && AutoEquipSettings.CurrentSettings.DeleteBlue) return true;
        return false;
    }
}
