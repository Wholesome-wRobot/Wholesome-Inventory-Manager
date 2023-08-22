using Wholesome_Inventory_Manager.Managers.Items;
using WholesomeToolbox;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.CharacterSheet
{
    internal class ClassSpecManager : IClassSpecManager
    {
        private bool _iAmCaster;
        public bool IAmCaster => _iAmCaster;

        public void Initialize()
        {
            AutoDetectSpec();
        }

        public void Dispose()
        {
        }

        public void AutoDetectSpec()
        {
            ClassSpec initialSpec = AutoEquipSettings.CurrentSettings.SelectedSpec;
            _iAmCaster = IsCaster(initialSpec);

            if (!AutoEquipSettings.CurrentSettings.AutoDetectStatWeights) return;

            ClassSpec mySpec = ClassSpec.None;

            switch (ObjectManager.Me.WowClass)
            {
                case (WoWClass.Warlock):
                    if (WTTalent.GetSpec() == 2)
                        mySpec = ClassSpec.WarlockDemonology;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.WarlockDestruction;
                    else
                        mySpec = ClassSpec.WarlockAffliction;
                    break;

                case (WoWClass.DeathKnight):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.DeathKnightBloodDPS;
                    else if (WTTalent.GetSpec() == 2)
                        mySpec = ClassSpec.DeathKnightFrostDPS;
                    else
                        mySpec = ClassSpec.DeathKnightUnholy;
                    break;

                case (WoWClass.Druid):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.DruidBalance;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.DruidRestoration;
                    else
                    {
                        // TBC FERAL
                        if (ToolBox.GetWoWVersion() == ToolBox.WoWVersion.TBC)
                        {
                            if (WTTalent.GetTalentRank(2, 7) > 0) // Feral Charge
                                mySpec = ClassSpec.DruidFeralTank;
                            else
                                mySpec = ClassSpec.DruidFeralDPS;
                        }
                        // WOTLK FERAL
                        if (ToolBox.GetWoWVersion() == ToolBox.WoWVersion.WOTLK)
                        {
                            if (WTTalent.GetTalentRank(2, 5) > 2 // Thick Hide
                                || WTTalent.GetTalentRank(2, 16) > 0 // Natural Reaction
                                || WTTalent.GetTalentRank(2, 22) > 0) // Protector of the Pack
                                mySpec = ClassSpec.DruidFeralTank;
                            else
                                mySpec = ClassSpec.DruidFeralDPS;
                        }
                    }
                    break;

                case (WoWClass.Hunter):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.HunterBeastMastery;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.HunterSurvival;
                    else
                        mySpec = ClassSpec.HunterMarksman;
                    break;

                case (WoWClass.Mage):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.MageArcane;
                    else if (WTTalent.GetSpec() == 2)
                        mySpec = ClassSpec.MageFire;
                    else
                        mySpec = ClassSpec.MageFrost;
                    break;

                case (WoWClass.Paladin):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.PaladinHoly;
                    else if (WTTalent.GetSpec() == 2)
                        mySpec = ClassSpec.PaladinProtection;
                    else
                        mySpec = ClassSpec.PaladinRetribution;
                    break;

                case (WoWClass.Priest):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.PriestDiscipline;
                    else if (WTTalent.GetSpec() == 2)
                        mySpec = ClassSpec.PriestHoly;
                    else
                        mySpec = ClassSpec.PriestShadow;
                    break;

                case (WoWClass.Rogue):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.RogueAssassination;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.RogueSubtelty;
                    else
                        mySpec = ClassSpec.RogueCombat;
                    break;

                case (WoWClass.Shaman):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.ShamanElemental;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.ShamanRestoration;
                    else
                        mySpec = ClassSpec.ShamanEnhancement;
                    break;

                case (WoWClass.Warrior):
                    if (WTTalent.GetSpec() == 1)
                        mySpec = ClassSpec.WarriorArms;
                    else if (WTTalent.GetSpec() == 3)
                        mySpec = ClassSpec.WarriorTank;
                    else
                        mySpec = ClassSpec.WarriorFury;
                    break;

                default:
                    mySpec = ClassSpec.None;
                    break;
            }

            // Update stat weights in case of auto detect
            if (initialSpec != mySpec)
            {
                Logger.Log($"Auto detected specialization {mySpec}");
                ItemCache.ClearCache(); // to Rescan all items
                SettingsPresets.ChangeStatsWeightSettings(mySpec);
                AutoEquipSettings.CurrentSettings.SelectedSpec = mySpec;
            }

            // Set other default plugin settings according to detected class for first launch
            if (AutoEquipSettings.CurrentSettings.FirstLaunch)
            {
                Logger.Log("First Launch");
                SettingsPresets.ChangeAutoEquipSetting(mySpec);
                AutoEquipSettings.CurrentSettings.FirstLaunch = false;
                AutoEquipSettings.CurrentSettings.Save();
            }

            _iAmCaster = IsCaster(mySpec);
        }

        private bool IsCaster(ClassSpec mySpec)
        {
            return mySpec == ClassSpec.DruidBalance
              || mySpec == ClassSpec.DruidRestoration
              || mySpec == ClassSpec.MageArcane
              || mySpec == ClassSpec.MageFire
              || mySpec == ClassSpec.MageFrost
              || mySpec == ClassSpec.PaladinHoly
              || mySpec == ClassSpec.PriestDiscipline
              || mySpec == ClassSpec.PriestHoly
              || mySpec == ClassSpec.PriestShadow
              || mySpec == ClassSpec.ShamanElemental
              || mySpec == ClassSpec.ShamanRestoration
              || mySpec == ClassSpec.WarlockAffliction
              || mySpec == ClassSpec.WarlockDemonology
              || mySpec == ClassSpec.WarlockDestruction;
        }
    }
}
