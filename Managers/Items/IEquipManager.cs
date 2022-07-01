using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal interface IEquipManager : ICycleable
    {
        (ISheetSlot, string) IsArmorBetter(ISheetSlot armorSlot, IWIMItem armorItem, bool isRoll = false);
        (ISheetSlot, string) IsRingBetter(IWIMItem ringItem, bool isRoll = false);
        (ISheetSlot, string) IsTrinketBetter(IWIMItem trinketItem, bool isRoll = false);
        (ISheetSlot, string) IsWeaponBetter(IWIMItem weaponToCheck, bool isRoll = false);
        string IsRangedBetter(IWIMItem rangedWeapon, bool isRoll = false);
        string IsAmmoBetter(IWIMItem ammo, List<IWIMItem> potentialAmmos);
        void CheckAll();
    }
}
