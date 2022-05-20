using System.Collections.Generic;
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
        private List<IWIMContainer> _listContainers = new List<IWIMContainer>();
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

            for (int i = 0; i < 5; i++)
            {
                string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
                if (!bagName.Equals(""))
                {
                    _listContainers.Add(new WIMContainer(i));
                }
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
                    MoveItemToBag(itemToMove, destination.BagPosition, destination.SlotIndex);
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

                if (newBag.InBag == bagToReplace.Position)
                {
                    newBag = GetAllBagItems().Find(b => b.ItemLink == newBag.ItemLink);
                }

                MoveItemToBag(newBag, bagToReplace.Position);

                _lootFilter.ProtectFromFilter(newBag.ItemLink);
                Lua.LuaDoString($"EquipPendingItem(0);");
                //Lua.LuaDoString($"StaticPopup1Button1:Click()");
            }
            Scan();
        }

        private List<int> GetEmptyContainerSlots()
        {
            List<int> result = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
                if (bagName.Equals(""))
                {
                    result.Add(i);
                }
            }
            return result;
        }

        private int GetNbBagEquipped() => _listContainers.FindAll(b => !b.IsAmmoPouch && !b.IsQuiver).Count;

        private IWIMItem GetBiggestBagFromBags()
        {
            return GetAllBagItems()
                    .FindAll(item => item.ItemType != "Recipe"
                        && item.BagCapacity > 0
                        && item.AmmoPouchCapacity == 0
                        && item.QuiverCapacity == 0)
                    .OrderByDescending(item => item.BagCapacity)
                    .FirstOrDefault();
        }

        private IWIMContainer GetSmallestEquippedBag()
        {
            return _listContainers
                    .FindAll(b => !b.IsOriginalBackpack && !b.IsAmmoPouch && !b.IsQuiver)
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
                    .FindAll(item => item.QuiverCapacity > 0)
                    .OrderByDescending(item => item.QuiverCapacity)
                    .FirstOrDefault();
            }
            else if (equippedRanged == TypeRanged.Guns.ToString())
            {
                bestAmmoContainerInBags = GetAllBagItems()
                    .FindAll(item => item.AmmoPouchCapacity > 0)
                    .OrderByDescending(item => item.AmmoPouchCapacity)
                    .FirstOrDefault();
            }
            return bestAmmoContainerInBags;
        }

        public void BagEquip()
        {
            if (AutoEquipSettings.CurrentSettings.AutoEquipBags)
            {
                bool ImHunterAndNeedAmmoBag = ObjectManager.Me.WowClass == WoWClass.Hunter && AutoEquipSettings.CurrentSettings.EquipQuiver;
                int maxAmountOfBags = ImHunterAndNeedAmmoBag ? 4 : 5;
                IWIMContainer equippedQuiver = _listContainers.Find(bag => bag.IsQuiver);
                IWIMContainer equippedAmmoPouch = _listContainers.Find(bag => bag.IsAmmoPouch);
                bool hasRangedWeaponEquipped = _characterSheetManager.RangedSlot.Item != null;
                string equippedRanged = _characterSheetManager.RangedSlot.Item?.ItemSubType;

                if (AutoEquipSettings.CurrentSettings.EquipQuiver)
                {
                    // Move ammoContainer to position 4
                    if (equippedQuiver != null && equippedQuiver.Position != 4)
                    {
                        equippedQuiver.MoveToSlot(4);
                        Scan();
                        equippedQuiver = _listContainers.Find(bag => bag.IsQuiver);
                    }
                    if (equippedAmmoPouch != null && equippedAmmoPouch.Position != 4)
                    {
                        equippedAmmoPouch.MoveToSlot(4);
                        Scan();
                        equippedAmmoPouch = _listContainers.Find(bag => bag.IsAmmoPouch);
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
                                    //Lua.LuaDoString($"StaticPopup1Button1:Click()");
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
                            int availableSpot = GetEmptyContainerSlots().First();
                            MoveItemToBag(biggestBag, availableSpot);

                            Lua.LuaDoString($"EquipPendingItem(0);");
                            //Lua.LuaDoString($"StaticPopup1Button1:Click()");
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
                        && smallestEquippedBag.Position != 0)
                        ReplaceBag(smallestEquippedBag, biggestBagInBags);
                }
            }
        }

        public List<IWIMItem> GetAllBagItems()
        {
            List<IWIMItem> result = new List<IWIMItem>();
            foreach (WIMContainer container in _listContainers)
            {
                result.AddRange(container.Items);
            }
            return result;
        }

        private bool MoveItemToBag(IWIMItem itemToMove, int bagPosition, int bagSlot)
        {
            Lua.LuaDoString($"PickupContainerItem({bagPosition}, {bagSlot});"); // en fait un clique sur le slot de destination
            ToolBox.Sleep(100);
            if (_listContainers.Find(bag => bag.Position == bagPosition).GetContainerItemlink(bagSlot) == itemToMove.ItemLink)
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

        public bool HaveBulletsInBags => GetAllBagItems().Exists(i => i.ItemSubType == "Bullet" && ObjectManager.Me.Level >= i.ItemMinLevel);
        public bool HaveArrowsInBags => GetAllBagItems().Exists(i => i.ItemSubType == "Arrow" && ObjectManager.Me.Level >= i.ItemMinLevel);
    }
}
