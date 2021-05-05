using System.Collections.Generic;

public static class WAEItemDB
{
    public static List<WAEItem> ItemDb = new List<WAEItem>();

    public static WAEItem Get(string itemLink)
    {
        return ItemDb.Find(it => it.ItemLink.Equals(itemLink));
    }

    public static void Add(WAEItem item)
    {
        if (ItemDb.Count > 300)
            ItemDb.RemoveRange(0, 100);

        if (!ContainsByItemLink(item.Name))
            ItemDb.Add(item);
    }

    public static bool ContainsByItemLink(string itemlink)
    {
        return ItemDb.Exists(it => it.ItemLink == itemlink);
    }
}