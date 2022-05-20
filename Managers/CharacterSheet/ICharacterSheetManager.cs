using System.Collections.Generic;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal interface ICharacterSheetManager : ICycleable
    {
        List<ISheetSlot> ArmorSlots { get; }
        List<ISheetSlot> FingerSlots { get; }
        List<ISheetSlot> TrinketSlots { get; }
        List<ISheetSlot> WeaponSlots { get; }
        ISheetSlot RangedSlot { get; }
        ISheetSlot AmmoSlot { get; }

        void Scan();
    }
}
