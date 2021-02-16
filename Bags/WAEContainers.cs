using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

public static class WAEContainers
{
    public static List<WAEContainer> ListContainers = new List<WAEContainer>();
    public static List<WAEItem> AllItems = new List<WAEItem>();

    private static void ReplaceBag(WAEContainer bagToReplace, WAEItem newBag)
    {
        Logger.Log($"Replacing {bagToReplace.ThisBag.Name} with {newBag.Name}");
        if (bagToReplace.EmptyBagInOtherBags())
        {
            if (newBag.InBag == bagToReplace.Position)
                newBag = AllItems.Find(b => b.ItemLink == newBag.ItemLink);
            newBag.MoveToBag(bagToReplace.Position);
        }
        Scan();
    }

    public static List<int> GetEmptyContainerSlots()
    {
        List<int> result = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
            if (bagName.Equals(""))
                result.Add(i);
        }
        return result;
    }

    public static void Scan()
    {
        Thread.Sleep(200);
        DateTime dateBegin = DateTime.Now;
        //Logger.LogDebug("*** Scanning bags...");

        ListContainers.Clear();
        AllItems.Clear();

        for (int i = 0; i < 5; i++)
        {
            string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
            if (!bagName.Equals(""))
                ListContainers.Add(new WAEContainer(i));
        }

        //Logger.LogDebug($"Bag Scan Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }

    private static int GetNbBagEquipped()
    {
        return ListContainers.FindAll(b => !b.IsAmmoPouch && !b.IsQuiver).Count;
    }

    public static WAEItem GetBiggestBagFromBags()
    {
        return AllItems
                .FindAll(item => item.BagCapacity > 0 && item.AmmoPouchCapacity == 0 && item.QuiverCapacity == 0)
                .OrderByDescending(item => item.BagCapacity)
                .FirstOrDefault();
    }

    public static WAEContainer GetSmallestEquippedBag()
    {
        return ListContainers
                    .FindAll(b => !b.IsOriginalBackpack && !b.IsAmmoPouch && !b.IsQuiver)
                    .OrderBy(bag => bag.Capacity)
                    .FirstOrDefault();
    }

    public static WAEItem GetBiggestAmmoContainerFromBags()
    {
        string equippedRanged = WAECharacterSheet.Ranged.Item?.ItemSubType;
        WAEItem bestAmmoContainerInBags = null;
        if (equippedRanged == TypeRanged.Bows.ToString() || equippedRanged == TypeRanged.Crossbows.ToString())
        {
            bestAmmoContainerInBags = AllItems
                .FindAll(item => item.QuiverCapacity > 0)
                .OrderByDescending(item => item.QuiverCapacity)
                .FirstOrDefault();
        }
        else if (equippedRanged == TypeRanged.Guns.ToString())
        {
            bestAmmoContainerInBags = AllItems
                .FindAll(item => item.AmmoPouchCapacity > 0)
                .OrderByDescending(item => item.AmmoPouchCapacity)
                .FirstOrDefault();
        }
        return bestAmmoContainerInBags;
    }

    public static void BagEquip()
    {
        //Logger.LogDebug("*** Bag equip...");
        DateTime dateBegin = DateTime.Now;

        if (AutoEquipSettings.CurrentSettings.AutoEquipBags)
        {
            bool ImHunterAndNeedAmmoBag = ObjectManager.Me.WowClass == WoWClass.Hunter && AutoEquipSettings.CurrentSettings.EquipQuiver;
            int maxAmountOfBags = ImHunterAndNeedAmmoBag ? 4 : 5;
            WAEContainer equippedQuiver = ListContainers.Find(bag => bag.IsQuiver);
            WAEContainer equippedAmmoPouch = ListContainers.Find(bag => bag.IsAmmoPouch);
            bool hasRangedWeaponEquipped = WAECharacterSheet.Ranged.Item != null;
            string equippedRanged = WAECharacterSheet.Ranged.Item?.ItemSubType;

            if (AutoEquipSettings.CurrentSettings.EquipQuiver)
            {
                // Move ammoContainer to position 4
                if (equippedQuiver != null && equippedQuiver.Position != 4)
                {
                    equippedQuiver.MoveToSlot(4);
                    Scan();
                    equippedQuiver = ListContainers.Find(bag => bag.IsQuiver);
                }
                if (equippedAmmoPouch != null && equippedAmmoPouch.Position != 4)
                {
                    equippedAmmoPouch.MoveToSlot(4);
                    Scan();
                    equippedAmmoPouch = ListContainers.Find(bag => bag.IsAmmoPouch);
                }

                // We have an ammo container equipped
                if (ImHunterAndNeedAmmoBag && (equippedQuiver != null || equippedAmmoPouch != null))
                {
                    WAEContainer equippedAmmoContainer = equippedQuiver == null ? equippedAmmoPouch : equippedQuiver;
                    WAEItem bestAmmoContainerInBags = GetBiggestAmmoContainerFromBags();

                    if (bestAmmoContainerInBags != null)
                    {
                        // Check we have the right type of ammo container
                        if ((equippedRanged == TypeRanged.Bows.ToString() || equippedRanged == TypeRanged.Crossbows.ToString()) && !equippedAmmoContainer.IsQuiver)
                            ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                        else if (equippedRanged == TypeRanged.Guns.ToString() && !equippedAmmoContainer.IsAmmoPouch)
                            ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                        // Try to find a better one
                        else if (bestAmmoContainerInBags.QuiverCapacity > equippedAmmoContainer.Capacity
                            || bestAmmoContainerInBags.AmmoPouchCapacity > equippedAmmoContainer.Capacity)
                            ReplaceBag(equippedAmmoContainer, bestAmmoContainerInBags);
                    }
                }

                // We have no ammo container equipped
                if (ImHunterAndNeedAmmoBag && equippedQuiver == null && equippedAmmoPouch == null)
                {
                    if (!hasRangedWeaponEquipped || equippedRanged == TypeRanged.Thrown.ToString())
                        maxAmountOfBags = 5;
                    else
                    {
                        WAEItem bestAmmoContainerInBags = GetBiggestAmmoContainerFromBags();
                        // We found an ammo container to equip
                        if (bestAmmoContainerInBags != null)
                        {
                            if (GetEmptyContainerSlots().Count > 0)
                            {
                                // There is an empty slot
                                int availableSpot = GetEmptyContainerSlots().Last();
                                Logger.Log($"Equipping {bestAmmoContainerInBags.Name} in slot {availableSpot}");
                                bestAmmoContainerInBags.MoveToBag(availableSpot);
                                Scan();
                            }
                            else
                            {
                                // No empty slot, we need to replace a bag by an ammo container
                                WAEContainer smallestEquippedBag = GetSmallestEquippedBag();
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
                    WAEContainer equippedAmmoContainer = equippedQuiver == null ? equippedAmmoPouch : equippedQuiver;
                    WAEItem biggestBagInBags = GetBiggestBagFromBags();
                    if (biggestBagInBags != null)
                        ReplaceBag(equippedAmmoContainer, biggestBagInBags);
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
                    WAEItem biggestBag = GetBiggestBagFromBags();

                    if (biggestBag != null)
                    {
                        Logger.Log($"Equipping {biggestBag.Name}");
                        int availableSpot = GetEmptyContainerSlots().First();
                        biggestBag.MoveToBag(availableSpot);
                        Scan();
                    }
                    if (GetNbBagEquipped() >= maxAmountOfBags)
                        break;
                }
            }

            // Bag equip to replace one for better capacity
            if (GetNbBagEquipped() >= maxAmountOfBags)
            {
                WAEContainer smallestEquippedBag = GetSmallestEquippedBag();
                WAEItem biggestBagInBags = GetBiggestBagFromBags();

                if (smallestEquippedBag != null
                    && biggestBagInBags != null
                    && smallestEquippedBag.Capacity < biggestBagInBags.BagCapacity 
                    && smallestEquippedBag.Position != 0)
                    ReplaceBag(smallestEquippedBag, biggestBagInBags);
            }
        }

        //Logger.LogDebug($"Bag Equip Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
    }
}
