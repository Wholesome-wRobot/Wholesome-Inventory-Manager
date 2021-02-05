using System.Collections.Generic;

public static class SessionItemDB
{
    public static List<Item> ItemDb = new List<Item>();

    public static Item Get(string itemLink)
    {
        return ItemDb.Find(it => it.ItemLink.Equals(itemLink));
    }

    public static void Add(Item item)
    {
        if (!ContainsByItemLink(item.Name))
            ItemDb.Add(item);
    }

    public static bool ContainsByItemLink(string itemlink)
    {
        return ItemDb.Exists(it => it.ItemLink == itemlink);
    }
}
