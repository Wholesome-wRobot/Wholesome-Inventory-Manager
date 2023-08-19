using System.Collections.Generic;
using System.Linq;
using WholesomeToolbox;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal class SkillsManager : ISkillsManager
    {
        private readonly object _SMlock = new object();
        public Dictionary<string, int> MySkills { get; } = new Dictionary<string, int>();
        public Spell DualWield { get; private set; }
        public bool KnowTitansGrip { get; private set; }
        public bool HasArmsAxesSpecialization { get; private set; }
        public bool HasArmsMacesSpecialization { get; private set; }
        public bool HasArmsSwordsSpecialization { get; private set; }
        public bool PrioritizeDaggers { get; private set; }

        public void Initialize()
        {
            RecordSkills();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
        }
        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
        }

        private void OnEventsLuaWithArgs(string id, List<string> args)
        {
            if (id == "SKILL_LINES_CHANGED" || id == "CHARACTER_POINTS_CHANGED")
            {
                RecordSkills();
            }
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

                DualWield = new Spell("Dual Wield");

                if (ObjectManager.Me.WowClass == wManager.Wow.Enums.WoWClass.Warrior)
                {
                    KnowTitansGrip = WTTalent.GetTalentRank(2, 27) > 0;
                    HasArmsAxesSpecialization = WTTalent.GetTalentRank(1, 13) > 0;
                    HasArmsMacesSpecialization = WTTalent.GetTalentRank(1, 15) > 0;
                    HasArmsSwordsSpecialization = WTTalent.GetTalentRank(1, 16) > 0;
                }

                if (ObjectManager.Me.WowClass == wManager.Wow.Enums.WoWClass.Rogue
                    && WTTalent.GetSpec() == 1)
                {
                    PrioritizeDaggers = true;
                }
            }
        }
    }
}
