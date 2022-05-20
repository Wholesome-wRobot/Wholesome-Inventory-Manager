using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal class WIMContainer : IWIMContainer
    {
        public int Position { get; private set; }
        public int Capacity { get; private set; }
        public bool IsAmmoPouch { get; private set; }
        public bool IsQuiver { get; private set; }
        public bool IsOriginalBackpack { get; private set; }
        public IWIMItem BagItem { get; private set; }
        public List<IWIMItem> Items { get; private set; } = new List<IWIMItem>();
        public List<IContainerSlot> Slots { get; private set; } = new List<IContainerSlot>();

        public WIMContainer(int position)
        {
            Position = position;
            Capacity = GetContainerNbSlots();
            if (Position != 0)
            {
                string bagItemLink = Lua.LuaDoString<string>($"return GetContainerItemLink(0, {Position - 4})");
                BagItem = new WIMItem(bagItemLink);
                IsQuiver = BagItem.QuiverCapacity > 0;
                IsAmmoPouch = BagItem.AmmoPouchCapacity > 0;
            }
            else
            {
                IsOriginalBackpack = true;
            }

            string[] allItems = GetAllItemLinks(Capacity).Split('$');
            for (int i = 0; i < allItems.Length; i++)
            {
                IWIMItem item;
                if (allItems[i] != "BAG")
                {
                    if (allItems[i] != "null")
                    {
                        item = new WIMItem(allItems[i], inBagSlot: i, inBag: Position);
                        Slots.Add(new ContainerSlot(i, position, item));
                        Items.Add(item);
                    }
                    else
                    {
                        Slots.Add(new ContainerSlot(i, position, null));
                    }
                }
            }
        }

        public void MoveToSlot(int position)
        {
            Lua.LuaDoString($"PickupBagFromSlot({Position + 19});");
            ToolBox.Sleep(100);
            Lua.LuaDoString($"PutItemInBag({position + 19});");
        }

        private string GetAllItemLinks(int capacity)
        {
            return Lua.LuaDoString<string>($@"
                    local allItems = ""BAG"";
                    for i=1,{capacity} do
                        local item = GetContainerItemLink({Position}, i);
                        if item == nil then item = ""null"" end;
                        allItems = allItems .. ""$"" .. item
                    end;
                    return allItems;
                ");
        }

        public int GetContainerNbSlots() => Lua.LuaDoString<int>($"return GetContainerNumSlots({Position});");
        public int GetContainerNbFreeSlots() => Lua.LuaDoString<int>($"local numberOfFreeSlots, BagType = GetContainerNumFreeSlots({Position}); return numberOfFreeSlots;");
        public string GetContainerItemlink(int slot) => Lua.LuaDoString<string>($"return GetContainerItemLink({Position}, {slot});");
    }
}
