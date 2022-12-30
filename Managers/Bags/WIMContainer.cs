using System.Collections.Generic;
using System.Diagnostics;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal class WIMContainer : IWIMContainer
    {
        public int Index { get; private set; }
        public int Capacity { get; private set; }
        public bool IsAmmoPouch { get; private set; }
        public bool IsQuiver { get; private set; }
        public bool IsOriginalBackpack { get; private set; }
        public IWIMItem BagItem { get; private set; }
        public SynchronizedCollection<IWIMItem> Items { get; private set; } = new SynchronizedCollection<IWIMItem>();
        public SynchronizedCollection<IContainerSlot> Slots { get; private set; } = new SynchronizedCollection<IContainerSlot>();

        public WIMContainer(string[] itemInfoArray, List<string[]> bagItems, int bagIndex)
        {
            Index = bagIndex;
            Capacity = bagItems.Count;
            if (Index != 0)
            {
                BagItem = new WIMItem(itemInfoArray, Index, 0);
                IsQuiver = BagItem.QuiverCapacity > 0;
                IsAmmoPouch = BagItem.AmmoPouchCapacity > 0;
            }
            else
            {
                IsOriginalBackpack = true;
            }

            for (int i = 0; i < Capacity; i++)
            {
                string[] itemInfos = bagItems[i];
                string itemLink = itemInfos[2];

                if (itemLink != "null")
                {
                    IWIMItem item = new WIMItem(bagItems[i], Index, i + 1);
                    Slots.Add(new ContainerSlot(i + 1, Index, item));
                    Items.Add(item);
                }
                else
                {
                    Slots.Add(new ContainerSlot(i + 1, Index, null));
                }

            }
        }

        public void MoveToSlot(int position)
        {
            Lua.LuaDoString($"PickupBagFromSlot({Index + 19});");
            ToolBox.Sleep(100);
            Lua.LuaDoString($"PutItemInBag({position + 19});");
        }

        public int GetContainerNbSlots() => Lua.LuaDoString<int>($"return GetContainerNumSlots({Index});");
        public int GetContainerNbFreeSlots() => Lua.LuaDoString<int>($"local numberOfFreeSlots, BagType = GetContainerNumFreeSlots({Index}); return numberOfFreeSlots;");
        public string GetContainerItemlink(int slot) => Lua.LuaDoString<string>($"return GetContainerItemLink({Index}, {slot});");
    }
}
