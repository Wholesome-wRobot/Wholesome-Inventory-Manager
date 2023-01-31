using System.Collections.Generic;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal interface IWIMItem
    {
        uint ItemId { get; }
        string Name { get; }
        string ItemLink { get; }
        int ItemRarity { get; }
        int ItemLevel { get; }
        int ItemMinLevel { get; }
        string ItemType { get; }
        string ItemSubType { get; }
        int ItemStackCount { get; }
        string ItemEquipLoc { get; }
        string ItemTexture { get; }
        int ItemSellPrice { get; }
        int BagCapacity { get; }
        int QuiverCapacity { get; }
        int AmmoPouchCapacity { get; }
        int BagIndex { get; }
        int SlotIndex { get; }
        double UniqueId { get; }
        float WeightScore { get; }
        float WeaponSpeed { get; }
        int RewardSlot { get; }
        int RollId { get; }
        bool HasBeenRolled { get; set; }
        bool IsUnique { get; set; }
        Dictionary<string, float> ItemStats { get; }

        void PickupFromBag();
        void DeleteFromBag(string reason);
        float GetOffHandWeightScore();
        void ClickInInventory(int slotId);
    }
}
