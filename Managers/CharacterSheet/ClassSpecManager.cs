using System.Collections.Generic;
using Wholesome_Inventory_Manager.Managers.Items;
using WholesomeToolbox;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal static class ClassSpecManager
    {
        public static ClassSpec MySpec { get; private set; }

        public static void Initialize()
        {
            DetectSpec();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
        }

        public static void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
        }

        private static void OnEventsLuaWithArgs(string id, List<string> args)
        {
            switch (id)
            {
                case "CHARACTER_POINTS_CHANGED":
                    DetectSpec();
                    break;
            }
        }

        public static void DetectSpec()
        {
            ClassSpec initialSpec = MySpec;
            
            switch (ObjectManager.Me.WowClass)
            {
                case (WoWClass.Warlock):
                    if (WTTalent.GetSpec() == 2)
                        MySpec = ClassSpec.WarlockDemonology;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.WarlockDestruction;
                    else
                        MySpec = ClassSpec.WarlockAffliction;
                    break;

                case (WoWClass.DeathKnight):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.DeathKnightBloodDPS;
                    else if (WTTalent.GetSpec() == 2)
                        MySpec = ClassSpec.DeathKnightFrostDPS;
                    else
                        MySpec = ClassSpec.DeathKnightUnholy;
                    break;

                case (WoWClass.Druid):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.DruidBalance;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.DruidRestoration;
                    else
                    {
                        // TBC FERAL
                        if (ToolBox.GetWoWVersion() == ToolBox.WoWVersion.TBC)
                        {
                            if (WTTalent.GetTalentRank(2, 7) > 0) // Feral Charge
                                MySpec = ClassSpec.DruidFeralTank;
                            else
                                MySpec = ClassSpec.DruidFeralDPS;
                        }
                        // WOTLK FERAL
                        if (ToolBox.GetWoWVersion() == ToolBox.WoWVersion.WOTLK)
                        {
                            if (WTTalent.GetTalentRank(2, 5) > 2 // Thick Hide
                                || WTTalent.GetTalentRank(2, 16) > 0 // Natural Reaction
                                || WTTalent.GetTalentRank(2, 22) > 0) // Protector of the Pack
                                MySpec = ClassSpec.DruidFeralTank;
                            else
                                MySpec = ClassSpec.DruidFeralDPS;
                        }
                    }
                    break;

                case (WoWClass.Hunter):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.HunterBeastMastery;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.HunterSurvival;
                    else
                        MySpec = ClassSpec.HunterMarksman;
                    break;

                case (WoWClass.Mage):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.MageArcane;
                    else if (WTTalent.GetSpec() == 2)
                        MySpec = ClassSpec.MageFire;
                    else
                        MySpec = ClassSpec.MageFrost;
                    break;

                case (WoWClass.Paladin):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.PaladinHoly;
                    else if (WTTalent.GetSpec() == 2)
                        MySpec = ClassSpec.PaladinProtection;
                    else
                        MySpec = ClassSpec.PaladinRetribution;
                    break;

                case (WoWClass.Priest):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.PriestDiscipline;
                    else if (WTTalent.GetSpec() == 2)
                        MySpec = ClassSpec.PriestHoly;
                    else
                        MySpec = ClassSpec.PriestShadow;
                    break;

                case (WoWClass.Rogue):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.RogueAssassination;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.RogueSubtelty;
                    else
                        MySpec = ClassSpec.RogueCombat;
                    break;

                case (WoWClass.Shaman):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.ShamanElemental;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.ShamanRestoration;
                    else
                        MySpec = ClassSpec.ShamanEnhancement;
                    break;

                case (WoWClass.Warrior):
                    if (WTTalent.GetSpec() == 1)
                        MySpec = ClassSpec.WarriorArms;
                    else if (WTTalent.GetSpec() == 3)
                        MySpec = ClassSpec.WarriorTank;
                    else
                        MySpec = ClassSpec.WarriorFury;
                    break;

                default:
                    MySpec = ClassSpec.None;
                    break;
            }
            
            // Update stat weights in case of auto detect
            if (AutoEquipSettings.CurrentSettings.AutoDetectStatWeights && initialSpec != MySpec)
            {
                ItemCache.ClearCache(); // to Rescan all items
                SettingsPresets.ChangeStatsWeightSettings(MySpec);
            }
            
            // Set other default plugin settings according to detected class for first launch
            if (AutoEquipSettings.CurrentSettings.FirstLaunch && initialSpec != MySpec)
            {
                Logger.Log("First Launch");
                SettingsPresets.ChangeAutoEquipSetting(MySpec);
                AutoEquipSettings.CurrentSettings.FirstLaunch = false;
                AutoEquipSettings.CurrentSettings.Save();
            }

            AutoEquipSettings.CurrentSettings.SpecSelectedByUser = MySpec;
        }

        public static bool ImACaster()
        {
            return MySpec == ClassSpec.DruidBalance
                || MySpec == ClassSpec.DruidRestoration
                || MySpec == ClassSpec.MageArcane
                || MySpec == ClassSpec.MageFire
                || MySpec == ClassSpec.MageFrost
                || MySpec == ClassSpec.PaladinHoly
                || MySpec == ClassSpec.PriestDiscipline
                || MySpec == ClassSpec.PriestHoly
                || MySpec == ClassSpec.PriestShadow
                || MySpec == ClassSpec.ShamanElemental
                || MySpec == ClassSpec.ShamanRestoration
                || MySpec == ClassSpec.WarlockAffliction
                || MySpec == ClassSpec.WarlockDemonology
                || MySpec == ClassSpec.WarlockDestruction;
        }
    }
}
