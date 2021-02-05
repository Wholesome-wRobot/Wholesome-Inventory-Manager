using System.Collections.Generic;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Container
{
    public int Position { get; set; }
    public int Capacity { get; set; }
    public List<Item> Items { get; set; } = new List<Item>();
    public List<ContainerSlot> Slots { get; set; } = new List<ContainerSlot>();
    public static List<Item> AllItemsInAllBags { get; set; } = new List<Item>();

    public Container(int position)
    {
        Position = position;
        Capacity = GetContainerNbSlots();
        for (int i = 1; i <= Capacity; i++)
        {
            string itemLink = GetContainerItemlink(i);
            if (itemLink.Length > 0 )
            {
                Item item = new Item(itemLink);
                item.IsInBagSlot = i;
                item.IsInBag = Position;
                Slots.Add(new ContainerSlot(i, position, item));
                Items.Add(item);
                AllItemsInAllBags.Add(item);
            }
            else
            {
                Slots.Add(new ContainerSlot(i, position, null));
            }
        }
    }

    public string GetContainerName()
    {
        string bagName = Lua.LuaDoString<string>($"return GetBagName({Position});");
        return bagName;
    }

    public int GetContainerNbSlots()
    {
        int numSlots = Lua.LuaDoString<int>($"return GetContainerNumSlots({Position});");
        return numSlots;
    }

    public int GetContainerNbFreeSlots()
    {
        int numFreeSlots = Lua.LuaDoString<int>($"local numberOfFreeSlots, BagType = GetContainerNumFreeSlots({Position}); return numberOfFreeSlots;");
        return numFreeSlots;
    }

    public int GetContainerItemId(int slot)
    {
        int itemId = Lua.LuaDoString<int>($"return GetContainerItemID({Position}, {slot});");
        return itemId;
    }

    public string GetContainerItemlink(int slot)
    {
        string itemLink = Lua.LuaDoString<string>($"return GetContainerItemLink({Position}, {slot});");
        return itemLink;
    }

    public string GetContainerItemName(int slot)
    {
        string itemLink = Lua.LuaDoString<string>($"return GetContainerItemLink({Position}, {slot});");
        return itemLink;
    }

    public string[] GetItemInfo(int ItemId)
    {
        string iteminfo = Lua.LuaDoString<string>($@"
                itemName, itemLink, itemRarity, itemLevel, itemMinLevel, itemType,
                itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo({ItemId})

                return itemName..'§'..itemLink..'§'..itemRarity..'§'..itemLevel..
                '§'..itemMinLevel..'§'..itemType..'§'..itemSubType..'§'..itemStackCount..
                '§'..itemEquipLoc..'§'..itemTexture..'§'..itemSellPrice");

        return iteminfo.Split('§');
    }

    public static int CountBagEquipped()
    {
        int result = 0;
        for (int i = 0; i < 5; i++)
        {
            string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
            if (!bagName.Equals(""))
                result++;
        }
        return result;
    }

    public static List<int> GetEmptyContainerSlots()
    {
        List<int> result = new List<int>();
        for(int i = 0; i < 5; i++)
        {
            string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
            if (bagName.Equals(""))
                result.Add(i);
        }
        return result;
    }
}
