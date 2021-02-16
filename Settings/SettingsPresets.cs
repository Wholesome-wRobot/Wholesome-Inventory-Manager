using System.Collections.Generic;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;
using static WAEEnums;

public class SettingsPresets
{
    public static void ChangeAutoEquipSetting(ClassSpec classSpec)
    {
        AutoEquipSettings.CurrentSettings.AutoEquipBags = true;
        AutoEquipSettings.CurrentSettings.AutoEquipGear = true;

        // Only change for restrictions, otherwise allow all
        // DK
        if (classSpec == ClassSpec.DeathKnightBloodDPS
            || classSpec == ClassSpec.DeathKnightBloodTank)
        {
            AutoEquipSettings.CurrentSettings.EquipOneHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
        }
        else if (classSpec == ClassSpec.DeathKnightFrostDPS
            || classSpec == ClassSpec.DeathKnightFrostTank)
        {
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
        } 
        // Hunter
        else if (ObjectManager.Me.WowClass == WoWClass.Hunter)
        {
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
        }
        // Paladin
        else if (classSpec == ClassSpec.PaladinRetribution)
        {
            AutoEquipSettings.CurrentSettings.EquipOneHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        else if (classSpec == ClassSpec.PaladinProtection || classSpec == ClassSpec.PaladinHoly)
        {
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        else if (ObjectManager.Me.WowClass == WoWClass.Rogue)
        {
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
        }
        // Shaman
        else if (classSpec == ClassSpec.ShamanElemental || classSpec == ClassSpec.ShamanRestoration)
        {
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        else if (classSpec == ClassSpec.ShamanEnhancement)
        {
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        // Warrior
        else if (classSpec == ClassSpec.WarriorFury)
        {
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
        }
        else if (classSpec == ClassSpec.WarriorTank)
        {
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = false;
        }
        else if (classSpec == ClassSpec.WarriorArms)
        {
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipOneHanders = false;
            AutoEquipSettings.CurrentSettings.EquipShields = false;
        }
        // Druid
        else if (ObjectManager.Me.WowClass == WoWClass.Druid)
        {
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        // Mage
        else if (ObjectManager.Me.WowClass == WoWClass.Mage)
        {
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        // Priest
        else if (ObjectManager.Me.WowClass == WoWClass.Priest)
        {
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }
        // Warlock
        else if (ObjectManager.Me.WowClass == WoWClass.Warlock)
        {
            AutoEquipSettings.CurrentSettings.EquipShields = false;
            AutoEquipSettings.CurrentSettings.EquipBows = false;
            AutoEquipSettings.CurrentSettings.EquipCrossbows = false;
            AutoEquipSettings.CurrentSettings.EquipGuns = false;
            AutoEquipSettings.CurrentSettings.EquipThrown = false;
        }

        AutoEquipSettings.CurrentSettings.Save();
        //AutoEquipSettings.Load();
    }

    public static void ChangeStatsWeightSettings(ClassSpec classSpec)
    {
        Logger.Log($"Setting stat weight to {classSpec}");

        if (!AllPresets.ContainsKey(classSpec))
        {
            Logger.LogError($"{classSpec} couldn't be found in the presets dictionary."); 
            return;
        }
        
        Dictionary<CharStat, int> preset = AllPresets[classSpec];

        if (preset == null)
        {
            Logger.LogError($"Preset dictionary for {classSpec} doesn't exist");
            return;
        }

        // Reset all values
        AutoEquipSettings.CurrentSettings.StatWeights.Clear();

        // Assign values
        foreach (KeyValuePair<CharStat, int> presetToAssign in preset)
            AutoEquipSettings.CurrentSettings.SetStat(presetToAssign.Key, presetToAssign.Value);

        AutoEquipSettings.CurrentSettings.Save();
        //AutoEquipSettings.Load();
    }

    private static Dictionary<ClassSpec, Dictionary<CharStat, int>> AllPresets { get; } = new Dictionary<ClassSpec, Dictionary<CharStat, int>>()
    {
        { ClassSpec.None, new Dictionary<CharStat, int>() { } },
        // DRUID
        { ClassSpec.DruidFeralTank, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 75},
                {CharStat.Agility, 100},
                {CharStat.Strength, 10},
                {CharStat.Armor, 10},
                {CharStat.AttackPower, 4},
                {CharStat.HitRating, 8},
                {CharStat.DefenseRating, 60},
                {CharStat.DodgeRating, 65},
                {CharStat.CriticalStrikeRating, 3},
                {CharStat.ExpertiseRating, 16},
                {CharStat.HasteRating, 5}
            }
        },
        { ClassSpec.DruidFeralDPS, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 800},
                {CharStat.Agility, 1000},
                {CharStat.Stamina, 1},
                //{CharStat.Armor, 1},
                {CharStat.HitRating, 500},
                {CharStat.CriticalStrikeRating, 550},
                {CharStat.HasteRating, 350},
                {CharStat.AttackPower, 400},
                {CharStat.AttackPowerinForms, 400},
                {CharStat.ExpertiseRating, 500},
                {CharStat.ArmorPenetrationRating, 900}
            }
        },
        { ClassSpec.DruidRestoration, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                //{CharStat.Armor, 1},
                {CharStat.Intellect, 510},
                {CharStat.Spirit, 320},
                {CharStat.CriticalStrikeRating, 110},
                {CharStat.HasteRating, 570},
                {CharStat.SpellPower, 1000},
                {CharStat.ManaPer5, 730},
            }
        },
        { ClassSpec.DruidBalance, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                //{CharStat.Armor, 1},
                {CharStat.Intellect, 220},
                {CharStat.Spirit, 220},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 430},
                {CharStat.HasteRating, 540},
                {CharStat.SpellPower, 660},
            }
        },
        // DK
        { ClassSpec.DeathKnightBloodDPS, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 990},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 910},
                {CharStat.CriticalStrikeRating, 570},
                {CharStat.HasteRating, 550},
                {CharStat.AttackPower, 360},
                {CharStat.ExpertiseRating, 900},
                {CharStat.ArmorPenetrationRating, 1000},
                {CharStat.Armor, 10},
                {CharStat.DamagePerSecond, 3600},
            }
        },
        { ClassSpec.DeathKnightBloodTank, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 31},
                {CharStat.Agility, 69},
                {CharStat.Stamina, 100},
                {CharStat.HitRating, 16},
                {CharStat.CriticalStrikeRating, 22},
                {CharStat.HasteRating, 16},
                {CharStat.AttackPower, 8},
                {CharStat.ExpertiseRating, 38},
                {CharStat.ArmorPenetrationRating, 26},
                {CharStat.Armor, 18},
                {CharStat.DefenseRating, 90},
                {CharStat.DodgeRating, 50},
                {CharStat.ParryRating, 43},
                {CharStat.DamagePerSecond, 500},
            }
        },
        { ClassSpec.DeathKnightFrostDPS, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 970},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 450},
                {CharStat.HasteRating, 280},
                {CharStat.AttackPower, 350},
                {CharStat.ExpertiseRating, 810},
                {CharStat.ArmorPenetrationRating, 610},
                {CharStat.Armor, 10},
                {CharStat.DamagePerSecond, 3370},
            }
        },
        { ClassSpec.DeathKnightFrostTank, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 96},
                {CharStat.Agility, 61},
                {CharStat.Stamina, 61},
                {CharStat.HitRating, 97},
                {CharStat.CriticalStrikeRating, 49},
                {CharStat.AttackPower, 41},
                {CharStat.ExpertiseRating, 69},
                {CharStat.ArmorPenetrationRating, 31},
                {CharStat.Armor, 5},
                {CharStat.DefenseRating, 85},
                {CharStat.DodgeRating, 61},
                {CharStat.ParryRating, 100},
                {CharStat.DamagePerSecond, 419},
            }
        },
        { ClassSpec.DeathKnightUnholy, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 1000},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 660},
                {CharStat.CriticalStrikeRating, 450},
                {CharStat.HasteRating, 480},
                {CharStat.AttackPower, 340},
                {CharStat.ExpertiseRating, 510},
                {CharStat.ArmorPenetrationRating, 320},
                {CharStat.Armor, 10},
                {CharStat.DamagePerSecond, 2090},
            }
        },
        // HUNTER
        { ClassSpec.HunterBeastMastery, new Dictionary<CharStat, int>()
            {
                {CharStat.Agility, 580},
                //{CharStat.Armor, 1},
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 370},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 400},
                {CharStat.HasteRating, 210},
                {CharStat.AttackPower, 300},
                {CharStat.ArmorPenetrationRating, 280},
                {CharStat.DamagePerSecond, 2130},
            }
        },
        { ClassSpec.HunterMarksman, new Dictionary<CharStat, int>()
            {
                {CharStat.Agility, 740},
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 390},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 570},
                {CharStat.HasteRating, 240},
                {CharStat.AttackPower, 320},
                {CharStat.ArmorPenetrationRating, 400},
                {CharStat.DamagePerSecond, 379},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.HunterSurvival, new Dictionary<CharStat, int>()
            {
                {CharStat.Agility, 760},
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 350},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 420},
                {CharStat.HasteRating, 310},
                {CharStat.AttackPower, 290},
                {CharStat.ArmorPenetrationRating, 260},
                {CharStat.DamagePerSecond, 1810},
               //{CharStat.Armor, 1},
            }
        },
        // MAGE
        { ClassSpec.MageArcane, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 340},
                {CharStat.Spirit, 140},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 370},
                {CharStat.HasteRating, 540},
                {CharStat.SpellPower, 490},
                {CharStat.FireSpellPower, 240},
                {CharStat.ArcaneSpellPower, 490},
                {CharStat.FrostSpellPower, 240},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.MageFire, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Spirit, 10},
                {CharStat.Intellect, 130},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 430},
                {CharStat.HasteRating, 530},
                {CharStat.SpellPower, 460},
                {CharStat.FireSpellPower, 460},
                {CharStat.ArcaneSpellPower, 230},
                {CharStat.FrostSpellPower, 230},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.MageFrost, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 60},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 190},
                {CharStat.HasteRating, 420},
                {CharStat.SpellPower, 390},
                {CharStat.FireSpellPower, 190},
                {CharStat.ArcaneSpellPower, 190},
                {CharStat.FrostSpellPower, 390},
                //{CharStat.Armor, 1},
            }
        },
        // PALADIN
        { ClassSpec.PaladinHoly, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 1000},
                {CharStat.CriticalStrikeRating, 460},
                {CharStat.HasteRating, 350},
                {CharStat.SpellPower, 580},
                {CharStat.ManaPer5, 880},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.PaladinProtection, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 16},
                {CharStat.Agility, 60},
                {CharStat.Stamina, 100},
                {CharStat.ExpertiseRating, 59},
                {CharStat.Armor, 8},
                {CharStat.BlockValue, 6},
                {CharStat.BlockRating, 7},
                {CharStat.DefenseRating, 45},
                {CharStat.DodgeRating, 55},
                {CharStat.ParryRating, 30},
            }
        },
        { ClassSpec.PaladinRetribution, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 800},
                {CharStat.Agility, 320},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 400},
                {CharStat.HasteRating, 300},
                {CharStat.AttackPower, 340},
                {CharStat.ExpertiseRating, 660},
                {CharStat.ArmorPenetrationRating, 220},
                {CharStat.SpellPower, 90},
                {CharStat.DamagePerSecond, 4700},
                //{CharStat.Armor, 1},
            }
        },
        // PRIEST
        { ClassSpec.PriestDiscipline, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 650},
                {CharStat.Spirit, 220},
                {CharStat.CriticalStrikeRating, 480},
                {CharStat.HasteRating, 590},
                {CharStat.SpellPower, 1000},
                {CharStat.ManaPer5, 670},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.PriestHoly, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 690},
                {CharStat.Spirit, 520},
                {CharStat.CriticalStrikeRating, 380},
                {CharStat.HasteRating, 310},
                {CharStat.SpellPower, 600},
                {CharStat.ManaPer5, 1000},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.PriestShadow, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 160},
                {CharStat.Spirit, 160},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 540},
                {CharStat.HasteRating, 500},
                {CharStat.SpellPower, 760},
                {CharStat.ShadowSpellPower, 760},
                //{CharStat.Armor, 1},
            }
        },
        // ROGUE
        { ClassSpec.RogueAssassination, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 550},
                {CharStat.Agility, 1000},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 830},
                {CharStat.CriticalStrikeRating, 810},
                {CharStat.HasteRating, 640},
                {CharStat.AttackPower, 650},
                {CharStat.ExpertiseRating, 870},
                {CharStat.ArmorPenetrationRating, 650},
                {CharStat.DamagePerSecond, 1700},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.RogueCombat, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 550},
                {CharStat.Agility, 1000},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 800},
                {CharStat.CriticalStrikeRating, 750},
                {CharStat.HasteRating, 730},
                {CharStat.AttackPower, 500},
                {CharStat.ExpertiseRating, 820},
                {CharStat.ArmorPenetrationRating, 1000},
                {CharStat.DamagePerSecond, 2200},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.RogueSubtelty, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 550},
                {CharStat.Agility, 1000},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 800},
                {CharStat.CriticalStrikeRating, 750},
                {CharStat.HasteRating, 750},
                {CharStat.AttackPower, 500},
                {CharStat.ExpertiseRating, 1000},
                {CharStat.ArmorPenetrationRating, 750},
                {CharStat.DamagePerSecond, 2280},
                //{CharStat.Armor, 1},
            }
        },
        // SHAMAN
        { ClassSpec.ShamanElemental, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 110},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 400},
                {CharStat.HasteRating, 560},
                {CharStat.SpellPower, 600},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.ShamanEnhancement, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 350},
                {CharStat.Agility, 550},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 550},
                {CharStat.HasteRating, 420},
                {CharStat.AttackPower, 320},
                {CharStat.ExpertiseRating, 840},
                {CharStat.ArmorPenetrationRating, 260},
                {CharStat.SpellPower, 290},
                {CharStat.DamagePerSecond, 1350},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.ShamanRestoration, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 850},
                {CharStat.CriticalStrikeRating, 620},
                {CharStat.HasteRating, 350},
                {CharStat.SpellPower, 770},
                {CharStat.ManaPer5, 1000},
                //{CharStat.Armor, 1},
            }
        },
        // WARLOCK
        { ClassSpec.WarlockAffliction, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 150},
                {CharStat.Spirit, 340},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 380},
                {CharStat.HasteRating, 610},
                {CharStat.SpellPower, 720},
                {CharStat.FireSpellPower, 360},
                {CharStat.ShadowSpellPower, 720},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.WarlockDemonology, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 130},
                {CharStat.Spirit, 290},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 310},
                {CharStat.HasteRating, 500},
                {CharStat.SpellPower, 450},
                {CharStat.FireSpellPower, 450},
                {CharStat.ShadowSpellPower, 450},
                //{CharStat.Armor, 1},
            }
        },
        { ClassSpec.WarlockDestruction, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 1},
                {CharStat.Intellect, 130},
                {CharStat.Spirit, 260},
                {CharStat.HitRating, 1000},
                {CharStat.CriticalStrikeRating, 160},
                {CharStat.HasteRating, 460},
                {CharStat.SpellPower, 470},
                {CharStat.FireSpellPower, 470},
                {CharStat.ShadowSpellPower, 230},
                //{CharStat.Armor, 1},
            }
        },
        // WARRIOR
        { ClassSpec.WarriorArms, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 1000},
                {CharStat.Agility, 650},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 900},
                {CharStat.CriticalStrikeRating, 800},
                {CharStat.HasteRating, 500},
                {CharStat.AttackPower, 450},
                {CharStat.ExpertiseRating, 850},
                {CharStat.ArmorPenetrationRating, 650},
                {CharStat.Armor, 10},
                {CharStat.DamagePerSecond, 1000},
            }
        },
        { ClassSpec.WarriorFury, new Dictionary<CharStat, int>()
            {
                {CharStat.Strength, 820},
                {CharStat.Agility, 530},
                {CharStat.Stamina, 1},
                {CharStat.HitRating, 480},
                {CharStat.CriticalStrikeRating, 660},
                {CharStat.HasteRating, 360},
                {CharStat.AttackPower, 310},
                {CharStat.ExpertiseRating, 1000},
                {CharStat.ArmorPenetrationRating, 520},
                {CharStat.Armor, 10},
                {CharStat.DamagePerSecond, 1000},
            }
        },
        { ClassSpec.WarriorTank, new Dictionary<CharStat, int>()
            {
                {CharStat.Stamina, 100},
                {CharStat.Agility, 67},
                {CharStat.Strength, 48},
                {CharStat.Armor, 6},
                {CharStat.AttackPower, 1},
                {CharStat.HitRating, 8},
                {CharStat.CriticalStrikeRating, 7},
                {CharStat.ExpertiseRating, 19},
                {CharStat.HasteRating, 1},
                {CharStat.ArmorPenetrationRating, 10},
                {CharStat.BlockValue, 81},
                {CharStat.BlockRating, 48},
                {CharStat.DefenseRating, 86},
                {CharStat.DodgeRating, 90},
                {CharStat.ParryRating, 67},
                {CharStat.DamagePerSecond, 10},
            }
        },
    };
}
