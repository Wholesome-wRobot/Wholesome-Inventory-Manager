using System.Collections.Generic;
using wManager.Wow.Class;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal interface ISkillsManager : ICycleable
    {
        Dictionary<string, int> MySkills { get; }
        void RecordSkills();
        Spell DualWield { get; }
        bool KnowTitansGrip { get; }
        bool HasArmsAxesSpecialization { get; }
        bool HasArmsMacesSpecialization { get; }
        bool HasArmsSwordsSpecialization { get; }
        bool PrioritizeDaggers { get; }
    }
}
