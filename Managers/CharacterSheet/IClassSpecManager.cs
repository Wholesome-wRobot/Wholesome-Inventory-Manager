namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal interface IClassSpecManager : ICycleable
    {
        void AutoDetectSpec();
        bool IAmCaster { get; }
    }
}
