using System.Collections.Generic;
using System.Linq;
using WholesomeToolbox;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal class SkillsManager : ISkillsManager
    {
        private readonly object _SMlock = new object();
        public Dictionary<string, int> MySkills { get; } = new Dictionary<string, int>();
        public bool KnowTitansGrip { get; private set; }
        public Spell DualWield { get; private set; }

        public void Initialize()
        {
            RecordSkills();
        }
        public void Dispose()
        {
        }

        public void RecordSkills()
        {
            lock (_SMlock)
            {
                MySkills.Clear();

                string luaListAllSkills = Lua.LuaDoString<string>($@"
                    local result = ""$"";
                    for i = 1, GetNumSkillLines() do
                        local skillName, header, isExpanded, skillRank, numTempPoints, skillModifier,
                            skillMaxRank, isAbandonable, stepCost, rankCost, minLevel, skillCostType,
                            skillDescription = GetSkillLineInfo(i)
                        result = result..skillName..""|""..skillRank..""$""
                    end
                    return result");

                List<string> skills = luaListAllSkills.Split('$').ToList();

                foreach (string skill in skills)
                {
                    if (skill.Length > 0)
                    {
                        string[] skillPair = skill.Split('|');
                        string skillName = skillPair[0]
                            .Replace("Shield", "Shields")
                            .Replace("Plate Mail", "Plate");
                        if (skillName == "Axes" || skillName == "Swords" || skillName == "Maces")
                            skillName = "One-Handed " + skillName;

                        if (!MySkills.ContainsKey(skillName))
                            MySkills.Add(skillName, int.Parse(skillPair[1]));
                    }
                }

                if (DualWield == null)
                {
                    DualWield = new Spell("Dual Wield");
                }

                if (!KnowTitansGrip && ClassSpecManager.MySpec == ClassSpec.WarriorFury)
                {
                    KnowTitansGrip = WTTalent.GetTalentRank(2, 27) > 0;
                }
            }
        }
    }
}
