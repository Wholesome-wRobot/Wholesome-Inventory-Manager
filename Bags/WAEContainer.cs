using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Helpers;

public class WAEContainer
{
    public int Position { get; set; }
    public int Capacity { get; set; }
    public bool IsAmmoPouch { get; set; }
    public bool IsQuiver { get; set; }
    public bool IsOriginalBackpack { get; set; }
    public WAEItem ThisBag { get; set; }
    public List<WAEItem> Items { get; set; } = new List<WAEItem>();
    public List<WAEContainerSlot> Slots { get; set; } = new List<WAEContainerSlot>();

    public WAEContainer(int position)
    {
        Position = position;
        Capacity = GetContainerNbSlots();
        if (Position != 0)
        {
            string bagItemLink = Lua.LuaDoString<string>($"return GetContainerItemLink(0, {Position - 4})");
            ThisBag = new WAEItem(bagItemLink);
            if (ThisBag.QuiverCapacity > 0)
                IsQuiver = true;
            if (ThisBag.AmmoPouchCapacity > 0)
                IsAmmoPouch = true;
        }
        else
            IsOriginalBackpack = true;

        string[] allItems = GetAllItemLinks(Capacity).Split('$');
        for (int i = 0; i < allItems.Length; i++)
        {
            WAEItem item;
            if (allItems[i] != "BAG")
            {
                if (allItems[i] != "null")
                {
                    item = new WAEItem(allItems[i]);
                    item.InBagSlot = i;
                    item.InBag = Position;
                    Slots.Add(new WAEContainerSlot(i, position, item));
                    Items.Add(item);
                    WAEBagInventory.AllItems.Add(item);
                }
                else
                {
                    Slots.Add(new WAEContainerSlot(i, position, null));
                }
            }
        }
    }

    public void MoveToSlot(int position)
    {
        Lua.LuaDoString($"PickupBagFromSlot({Position + 19});");
        Thread.Sleep(50);
        Lua.LuaDoString($"PutItemInBag({position + 19});");
        Thread.Sleep(50);
        WAEBagInventory.Scan();
    }

    public bool EmptyBagInOtherBags()
    {
        List<WAEContainerSlot> freeSlots = new List<WAEContainerSlot>();

        // record free slots
        foreach (WAEContainer container in WAEBagInventory.ListContainers.Where(b => b.IsOriginalBackpack || !b.IsAmmoPouch && !b.IsQuiver))
        {
            if (container != this)
                freeSlots.AddRange(container.Slots.Where(slot => slot.OccupiedBy == null));
        }

        // Move Items
        if (freeSlots.Count > Items.Count)
        {
            Logger.Log($"Moving items out of {ThisBag.Name}");
            for (int i = 0; i < Items.Count; i++)
            {
                WAEContainerSlot destination = freeSlots[i];
                WAEItem smallBag = Items[i];
                smallBag.PickupFromBag();
                Thread.Sleep(100);
                smallBag.MoveToBag(destination.BagPosition, destination.Slot);
                Thread.Sleep(100);
            }
        }

        WAEBagInventory.Scan();

        // Check if bag to move is actually empty
        if (GetContainerNbFreeSlots() == GetContainerNbSlots())
            return true;

        return false;
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

    public string GetContainerItemlink(int slot)
    {
        string itemLink = Lua.LuaDoString<string>($"return GetContainerItemLink({Position}, {slot});");
        return itemLink;
    }
}