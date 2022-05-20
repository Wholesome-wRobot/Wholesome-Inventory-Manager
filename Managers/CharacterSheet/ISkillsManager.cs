using System.Collections.Generic;
using wManager.Wow.Class;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal interface ISkillsManager : ICycleable
    {
        Dictionary<string, int> MySkills { get; }
        void RecordSkills();
        bool KnowTitansGrip { get; }
        Spell DualWield { get; }
    }
}
