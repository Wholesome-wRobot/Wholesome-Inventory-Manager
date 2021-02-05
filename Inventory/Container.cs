using System.Collections.Generic;
using wManager.Wow.Helpers;

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
        string[] allItems = GetAllItemLinks(Capacity).Split('$');
        for (int i = 0; i < allItems.Length; i++)
        {
            Item item;
            if (allItems[i] != "BAG")
            {
                if (allItems[i] != "null")
                {
                    item = new Item(allItems[i]);
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
            //Logger.Log(i.ToString() + " - " + allItems[i]);
        }
    }

    private string GetAllItemLinks(int capacity)
    {
        string allItems = Lua.LuaDoString<string>($@"
                                local allItems = ""BAG"";
                                for i=1,{capacity} do
                                    local item = GetContainerItemLink({Position}, i);
                                    if item == nil then item = ""null"" end;
                                    allItems = allItems .. ""$"" .. item
                                end;
                                return allItems;");
        return allItems;
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
