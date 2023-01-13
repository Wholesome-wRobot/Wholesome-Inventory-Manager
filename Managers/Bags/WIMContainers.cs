using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Filter;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.Bags
{
    internal class WIMContainers : IWIMContainers
    {
        private SynchronizedCollection<IWIMContainer> _listContainers = new SynchronizedCollection<IWIMContainer>();
        private readonly ILootFilter _lootFilter;
        private readonly ICharacterSheetManager _characterSheetManager;

        public WIMContainers(ICharacterSheetManager characterSheetManager, ILootFilter lootFilter)
        {
            _lootFilter = lootFilter;
            _characterSheetManager = characterSheetManager;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        public void Scan()
        {
            _listContainers.Clear();

            // This LUA command returns all items info in an array
            string[] luaItemInfos = Lua.LuaDoString<string[]>($@"
                local result = {{}};
                for i=0, 4, 1 do
                    local bagLink;
                    if i == 0 then
                        bagLink = ""|cff1eff00|Hitem:4500:0:0:0:0:0:0:0:0|h[Traveler's Backpack]|h|r"";
                    else
                        bagLink = GetContainerItemLink(0, i-4);
                    end
                    -- bags 0 to 4 (right to left)
                    if (bagLink ~= nil) then
                        local containerNbSlots = GetContainerNumSlots(i)
                        -- Add bag first
                        table.insert(result, ParseItemInfo(i, 0, bagLink));
                        -- Get all items in bag
                        for j=1, containerNbSlots do
                            local itemInfoTable = {{}};
                            local itemLink = GetContainerItemLink(i, j);
                            if itemLink == nil then 
                                table.insert(result, table.concat({{ i, j, ""null"" }}, ""£""));
                            else
                                table.insert(result, ParseItemInfo(i, j, itemLink));
                            end;
                        end;
                    end
                end
                return unpack(result);
            ");

            // Dump and unpack all items infos
            List<string[]> allItemInfos = new List<string[]>();
            if (luaItemInfos.Length > 0 && !string.IsNullOrEmpty(luaItemInfos[0]))
            {
                foreach (string luaItemInfo in luaItemInfos)
                {
                    allItemInfos.Add(luaItemInfo.Split('£'));
                }

                // Create bags
                for (int i = 0; i < 5; i++)
                {
                    string[] bag = allItemInfos.Find(itemInfo => itemInfo[1] == "0" && ToolBox.ParseInt(itemInfo[0]) == i);
                    if (bag != null)
                    {
                        List<string[]> itemsInThisBag = allItemInfos
                            .FindAll(itemInfo => itemInfo[1] != "0" && ToolBox.ParseInt(itemInfo[0]) == i);
                        _listContainers.Add(new WIMContainer(bag, itemsInThisBag, i));
                    }
                }
            }
            else
            {
                Logger.LogError($"[Containers] LUA info was empty");
            }
        }

        public bool EmptyBagInOtherBags(IWIMContainer bagToEmpty)
        {
            List<IContainerSlot> freeSlots = new List<IContainerSlot>();

            // record free slots
            foreach (IWIMContainer container in _listContainers.Where(b => b.IsOriginalBackpack || !b.IsAmmoPouch && !b.IsQuiver))
            {
                if (container != bagToEmpty)
                {
                    freeSlots.AddRange(container.Slots.Where(slot => slot.OccupiedBy == null));
                }
            }

            // Move Items
            if (freeSlots.Count > bagToEmpty.Items.Count)
            {
                Logger.Log($"Moving items out of {bagToEmpty.BagItem.Name}");
                for (int i = 0; i < bagToEmpty.Items.Count; i++)
                {
                    IContainerSlot destination = freeSlots[i];
                    IWIMItem itemToMove = bagToEmpty.Items[i];
                    itemToMove.PickupFromBag();
                    ToolBox.Sleep(100);
                    MoveItemToBag(itemToMove, destination.BagIndex, destination.SlotIndex);
                    ToolBox.Sleep(100);
                }
            }

            return bagToEmpty.GetContainerNbFreeSlots() == bagToEmpty.GetContainerNbSlots();
        }

        private void ReplaceBag(IWIMContainer bagToReplace, IWIMItem newBag)
        {
            if (EmptyBagInOtherBags(bagToReplace))
            {
                Logger.Log($"Replacing {bagToReplace.BagItem.Name} with {newBag.Name}");

                if (newBag.BagIndex == bagToReplace.Index)
                {
                    newBag = GetAllBagItems().FirstOrDefault(b => b.ItemLink == newBag.ItemLink);
                }

                MoveItemToBag(newBag, bagToReplace.Index);

                _lootFilter.ProtectFromFilter(newBag.ItemLink);
                Lua.LuaDoString($"EquipPendingItem(0);");
            }
            Scan();
        }

        private List<int> GetEmptyContainerSlots()
        {
            List<int> emptys = new List<int>();
            string[] result = Lua.LuaDoString<string[]>($@"
                local result = {{}}
                for i = 0, 4 do 
                    if GetBagName(i) == nil then
                        table.insert(result, i);
                    end
                end
                return unpack(result);
            ");
            foreach(string ind in result)
            {
                if (int.TryParse(ind, out int index))
                {
                    emptys.Add(index);
                }
                else
                {
                    Logger.LogError($"Couldn't parse empty bag result {ind}");
                }
            }
            return emptys;
        }

        private int GetNbBagEquipped()
        {
            return _listContainers.Where(b => !b.IsAmmoPouch && !b.IsQuiver).Count();
        }

        private IWIMItem GetBiggestBagFromBags()
        {
            return GetAllBagItems()
                    .Where(item => item.ItemType != "Recipe"
                        && item.BagCapacity > 0
                        && item.AmmoPouchCapacity == 0
                        && item.QuiverCapacity == 0)
                    .OrderByDescending(item => item.BagCapacity)
                    .FirstOrDefault();
        }

        private IWIMContainer GetSmallestEquippedBag()
        {
            return _listContainers
                .Where(b => !b.IsOriginalBackpack && !b.IsAmmoPouch && !b.IsQuiver)
                .OrderBy(bag => bag.Capacity)
                .FirstOrDefault();
        }

        private IWIMItem GetBiggestAmmoContainerFromBags()
        {
            string equippedRanged = _characterSheetManager.RangedSlot.Item?.ItemSubType;
            IWIMItem bestAmmoContainerInBags = null;
            if (equippedRanged == TypeRanged.Bows.ToString() || equippedRanged == TypeRanged.Crossbows.ToString())
            {
                bestAmmoContainerInBags = GetAllBagItems()
                    .Where(item => item.QuiverCapacity > 0)
                    .OrderByDescending(item => item.QuiverCapacity)
                    .FirstOrDefault();
            }
            else if (equippedRanged == TypeRanged.Guns.ToString())
            {
                bestAmmoContainerInBags = GetAllBagItems()
                    .Where(item => item.AmmoPouchCapacity > 0)
                    .OrderByDescending(item => item.AmmoPouchCapacity)
                    .FirstOrDefault();
            }
            return bestAmmoContainerInBags;
        }

        public void BagEquip()
        {
            if (AutoEquipSettings.CurrentSettings.AutoEquipBags)
            {
                Stopwatch watch = Stopwatch.StartNew();

                bool ImHunterAndNeedAmmoBag = ObjectManager.Me.WowClass == WoWClass.Hunter && AutoEquipSettings.CurrentSettings.EquipQuiver;
                int maxAmountOfBags = ImHunterAndNeedAmmoBag ? 4 : 5;
                IWIMContainer equippedQuiver = _listContainers.FirstOrDefault(bag => bag.IsQuiver);
                IWIMContainer equippedAmmoPouch = _listContainers.FirstOrDefault(bag => bag.IsAmmoPouch);
                bool hasRangedWeaponEquipped = _characterSheetManager.RangedSlot.Item != null;
                string equippedRanged = _characterSheetManager.RangedSlot.Item?.ItemSubType;

                if (AutoEquipSettings.CurrentSettings.EquipQuiver)
                {
                    // Move ammoContainer to position 4
                    if (equippedQuiver != null && equippedQuiver.Index != 4)
                    {
                        equippedQuiver.MoveToSlot(4);
                        Scan();
                        equippedQuiver = _listContainers.FirstOrDefault(bag => bag.IsQuiver);
                    }
                    if (equippedAmmoPouch != null && equippedAmmoPouch.Index != 4)
                    {
                        equippedAmmoPouch.MoveToSlot(4);
                        Scan();
                        equippedAmmoPouch = _listContainers.FirstOrDefault(bag => bag.IsAmmoPouch);
                    }

                    // We have an ammo container equipped
                    if (ImHunterAndNeedAmmoBag && (equippedQuiver != null || equippedAmmoPouch != null))
                    {
                        IWIMContainer equippedAmmoContainer = equippedQuiver == null ? equippedAmmoPouch : equippedQuiver;
                        IWIMItem bestAmmoContainerInBags = GetBiggestAmmoContainerFromBags();

                        if (bestAmmoContainerInBags != null)
                        {
                            // Check we have the right type of ammo container
                            if ((equippedRanged == TypeRanged.Bows.ToString() || equippedRanged == TypeRanged.Crossbows.ToString()) && !equippedAmmoContainer.IsQuiver)
                            {
                                ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                            }
                            else if (equippedRanged == TypeRanged.Guns.ToString() && !equippedAmmoContainer.IsAmmoPouch)
                            {
                                ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                            }
                            // Try to find a better one
                            else if (bestAmmoContainerInBags.QuiverCapacity > equippedAmmoContainer.Capacity
                                || bestAmmoContainerInBags.AmmoPouchCapacity > equippedAmmoContainer.Capacity)
                            {
                                ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                            }
                        }
                    }

                    // We have no ammo container equipped
                    if (ImHunterAndNeedAmmoBag && equippedQuiver == null && equippedAmmoPouch == null)
                    {
                        if (!hasRangedWeaponEquipped || equippedRanged == TypeRanged.Thrown.ToString())
                        {
                            maxAmountOfBags = 5;
                        }
                        else
                        {
                            IWIMItem bestAmmoContainerInBags = GetBiggestAmmoContainerFromBags();
                            // We found an ammo container to equip
                            if (bestAmmoContainerInBags != null)
                            {
                                if (GetEmptyContainerSlots().Count > 0)
                                {
                                    // There is an empty slot
                                    int availableSpot = GetEmptyContainerSlots().Last();
                                    Logger.Log($"Equipping {bestAmmoContainerInBags.Name} in slot {availableSpot}");
                                    MoveItemToBag(bestAmmoContainerInBags, availableSpot);

                                    Lua.LuaDoString($"EquipPendingItem(0);");
                                    _lootFilter.ProtectFromFilter(bestAmmoContainerInBags.ItemLink);

                                    Scan();
                                }
                                else
                                {
                                    // No empty slot, we need to replace a bag by an ammo container
                                    IWIMContainer smallestEquippedBag = GetSmallestEquippedBag();
                                    ReplaceBag(smallestEquippedBag, bestAmmoContainerInBags);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // The user doesn't want to have quiver equipped, removing them
                    if (equippedQuiver != null || equippedAmmoPouch != null)
                    {
                        IWIMContainer equippedAmmoContainer = equippedQuiver == null ? equippedAmmoPouch : equippedQuiver;
                        IWIMItem biggestBagInBags = GetBiggestBagFromBags();
                        if (biggestBagInBags != null)
                        {
                            ReplaceBag(equippedAmmoContainer, biggestBagInBags);
                        }
                    }
                }

                // Bag equip if we have at least 1 empty slot
                if (GetNbBagEquipped() < maxAmountOfBags)
                {
                    List<int> emptyContainerSlots = GetEmptyContainerSlots();
                    int nbEmpty = emptyContainerSlots.Count;
                    int nbloop = emptyContainerSlots.Count;

                    foreach (int emptySlotId in emptyContainerSlots)
                    {
                        IWIMItem biggestBag = GetBiggestBagFromBags();

                        if (biggestBag != null)
                        {
                            Logger.Log($"Equipping {biggestBag.Name}");
                            int availableSpot = GetEmptyContainerSlots().FirstOrDefault();
                            MoveItemToBag(biggestBag, availableSpot);

                            Lua.LuaDoString($"EquipPendingItem(0);");
                            _lootFilter.ProtectFromFilter(biggestBag.ItemLink);

                            Scan();
                        }

                        if (GetNbBagEquipped() >= maxAmountOfBags)
                        {
                            break;
                        }
                    }
                }

                // Bag equip to replace one for better capacity
                if (GetNbBagEquipped() >= maxAmountOfBags)
                {
                    IWIMContainer smallestEquippedBag = GetSmallestEquippedBag();
                    IWIMItem biggestBagInBags = GetBiggestBagFromBags();

                    if (smallestEquippedBag != null
                        && biggestBagInBags != null
                        && smallestEquippedBag.Capacity < biggestBagInBags.BagCapacity
                        && smallestEquippedBag.Index != 0)
                        ReplaceBag(smallestEquippedBag, biggestBagInBags);
                }
            }
        }

        public SynchronizedCollection<IWIMItem> GetAllBagItems()
        {
            SynchronizedCollection<IWIMItem> result = new SynchronizedCollection<IWIMItem>();
            foreach (WIMContainer container in _listContainers)
            {
                foreach (IWIMItem item in container.Items)
                    result.Add(item);
            }
            return result;
        }

        private bool MoveItemToBag(IWIMItem itemToMove, int bagPosition, int bagSlot)
        {
            Lua.LuaDoString($"PickupContainerItem({bagPosition}, {bagSlot});"); // en fait un clique sur le slot de destination
            ToolBox.Sleep(100);
            if (_listContainers.FirstOrDefault(bag => bag.Index == bagPosition).GetContainerItemlink(bagSlot) == itemToMove.ItemLink)
            {
                return true;
            }
            Logger.LogError($"Couldn't move {itemToMove.Name} to bag {bagPosition} slot {bagSlot}, retrying soon.");
            return false;
        }

        private void MoveItemToBag(IWIMItem itemToMove, int position)
        {
            itemToMove.PickupFromBag();
            ToolBox.Sleep(100);
            int bagSlot = 19 + position;
            Lua.LuaDoString($"PutItemInBag({bagSlot})");
            ToolBox.Sleep(100);
        }

        public bool HaveBulletsInBags => GetAllBagItems().FirstOrDefault(i => i.ItemSubType == "Bullet" && ObjectManager.Me.Level >= i.ItemMinLevel) != null;
        public bool HaveArrowsInBags => GetAllBagItems().FirstOrDefault(i => i.ItemSubType == "Arrow" && ObjectManager.Me.Level >= i.ItemMinLevel) != null;
    }
}
