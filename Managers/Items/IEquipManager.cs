using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal interface IEquipManager : ICycleable
    {
        (ISheetSlot, string) IsArmorBetter(ISheetSlot armorSlot, IWIMItem armorItem);
        (ISheetSlot, string) IsRingBetter(IWIMItem ringItem);
        (ISheetSlot, string) IsTrinketBetter(IWIMItem trinketItem);
        (ISheetSlot, string) IsWeaponBetter(IWIMItem weaponToCheck);
        string IsRangedBetter(IWIMItem rangedWeapon);
        string IsAmmoBetter(IWIMItem ammo, List<IWIMItem> potentialAmmos);
        void CheckAll();
    }
}
