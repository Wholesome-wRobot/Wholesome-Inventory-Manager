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
        if (!ContainsByItemLink(item.Name))
            ItemDb.Add(item);
    }

    public static bool ContainsByItemLink(string itemlink)
    {
        return ItemDb.Exists(it => it.ItemLink == itemlink);
    }
}