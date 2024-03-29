﻿using System.Collections.Generic;
using wManager.Wow.Enums;

public class WAEEnums
{
    public enum TypeRanged
    {
        None,
        Bows,
        Thrown,
        Crossbows,
        Guns
    }

    public enum ClassSpec
    {
        None,
        WarlockAffliction,
        WarlockDemonology,
        WarlockDestruction,
        DeathKnightFrostDPS,
        DeathKnightFrostTank,
        DeathKnightBloodDPS,
        DeathKnightBloodTank,
        DeathKnightUnholy,
        DruidBalance,
        DruidFeralTank,
        DruidFeralDPS,
        DruidRestoration,
        HunterBeastMastery,
        HunterMarksman,
        HunterSurvival,
        MageArcane,
        MageFire,
        MageFrost,
        PaladinHoly,
        PaladinRetribution,
        PaladinProtection,
        PriestDiscipline,
        PriestHoly,
        PriestShadow,
        RogueAssassination,
        RogueCombat,
        RogueSubtelty,
        ShamanElemental,
        ShamanEnhancement,
        ShamanRestoration,
        WarriorArms,
        WarriorFury,
        WarriorTank
    }

    public enum CharStat
    {
        Stamina,
        Intellect,
        Agility,
        Strength,
        Spirit,
        Armor,
        AttackPower,
        HitRating,
        ManaPer5,
        DamagePerSecond,
        BlockValue,
        BlockRating,
        DefenseRating,
        SpellPower,
        DodgeRating,
        CriticalStrikeRating,
        ExpertiseRating,
        HasteRating,
        ArmorPenetrationRating,
        ParryRating,
        ResilienceRating,
        SpellPenetration,
        AttackPowerinForms,
        FireSpellPower,
        ShadowSpellPower,
        ArcaneSpellPower,
        FrostSpellPower
    }

    public static Dictionary<string, CharStat> StatEnums { get; } = new Dictionary<string, CharStat>()
    {
        // TBC
        { "damage and healing done by magical spells", CharStat.SpellPower },
        { "for all magical spells and effects", CharStat.SpellPower },
        { "Your attacks ignore", CharStat.ArmorPenetrationRating },
        { "Spell Damage", CharStat.SpellPower },
        { "Spell Critical", CharStat.SpellPenetration },
        // WotLK
        { "Stamina", CharStat.Stamina },
        { "Intellect", CharStat.Intellect },
        { "Agility", CharStat.Agility },
        { "Strength", CharStat.Strength },
        { "Spirit", CharStat.Spirit },
        { "Armor", CharStat.Armor },
        { "Attack Power", CharStat.AttackPower },
        { "Hit Rating", CharStat.HitRating },
        { "Mana Per 5 Sec.", CharStat.ManaPer5 },
        { "Damage Per Second", CharStat.DamagePerSecond },
        { "Block Value", CharStat.BlockValue },
        { "Block Rating", CharStat.BlockRating },
        { "Defense Rating", CharStat.DefenseRating },
        { "Spell Power", CharStat.SpellPower },
        { "Dodge Rating", CharStat.DodgeRating },
        { "Critical Strike Rating", CharStat.CriticalStrikeRating },
        { "Expertise Rating", CharStat.ExpertiseRating },
        { "Haste Rating", CharStat.HasteRating },
        { "Armor Penetration Rating", CharStat.ArmorPenetrationRating },
        { "Parry Rating", CharStat.ParryRating },
        { "Resilience Rating", CharStat.ResilienceRating },
        { "Spell Penetration", CharStat.SpellPenetration },
        { "Attack Power In Forms", CharStat.AttackPowerinForms },
        { "Fire Spell Power", CharStat.FireSpellPower },
        { "Shadow Spell Power", CharStat.ShadowSpellPower },
        { "Arcane Spell Power", CharStat.ArcaneSpellPower },
        { "Frost Spell Power", CharStat.FrostSpellPower },
        // TBC backup
        { "Block", CharStat.BlockValue },
    };

    public static Dictionary<string, SkillLine> ItemSkillsDictionary { get; set; } = new Dictionary<string, SkillLine>
    {
        { "Miscellaneous", SkillLine.Unarmed },
        { "Shields", SkillLine.Shield },
        { "Fist Weapons", SkillLine.FistWeapons },
        { "Daggers", SkillLine.Daggers },
        { "Staves", SkillLine.Staves },
        { "Polearms", SkillLine.Polearms },
        { "One-Handed Swords", SkillLine.Swords },
        { "Two-Handed Swords", SkillLine.TwoHandedSwords },
        { "One-Handed Axes", SkillLine.Axes },
        { "Two-Handed Axes", SkillLine.TwoHandedAxes },
        { "One-Handed Maces", SkillLine.Maces },
        { "Two-Handed Maces", SkillLine.TwoHandedMaces },
        { "Bows", SkillLine.Bows },
        { "Guns", SkillLine.Guns },
        { "Crossbows", SkillLine.Crossbows },
        { "Wands", SkillLine.Wands },
        { "Thrown", SkillLine.Thrown },
        { "Cloth", SkillLine.Cloth },
        { "Leather", SkillLine.Leather },
        { "Mail", SkillLine.Mail },
        { "Plate", SkillLine.PlateMail },
        // IGNORE
        { "Librams", SkillLine.None },
        { "Totems", SkillLine.None },
        { "Idols", SkillLine.None },
        { "Sigils", SkillLine.None },
    };

    public static List<SkillLine> OneHanders { get; set; } = new List<SkillLine>()
    {
        SkillLine.Shield,
        SkillLine.FistWeapons,
        SkillLine.Daggers,
        SkillLine.Swords,
        SkillLine.Axes,
        SkillLine.Maces
    };

    public static List<SkillLine> OneHanderWeapons { get; set; } = new List<SkillLine>()
    {
        SkillLine.FistWeapons,
        SkillLine.Daggers,
        SkillLine.Swords,
        SkillLine.Axes,
        SkillLine.Maces
    };

    public static List<SkillLine> TwoHanders { get; set; } = new List<SkillLine>()
    {
        SkillLine.TwoHandedAxes,
        SkillLine.TwoHandedMaces,
        SkillLine.TwoHandedSwords,
        SkillLine.Staves,
        SkillLine.Polearms
    };

    public static Dictionary<uint, int> TBCBags { get; } = new Dictionary<uint, int>()
    {
        { 38082, 22 },
        { 4981, 12 },
        { 5765, 10 },
        { 856, 15 },
        { 14156, 18 },
        { 4498, 8 },
        { 3343, 8 },
        { 22571, 6 },
        { 19291, 14 },
        { 10959, 16 },
        { 918, 10 },
        { 30744, 14 },
        { 23389, 4 },
        { 10683, 16 },
        { 11324, 14 },
        { 16057, 12 },
        { 932, 10 },
        { 3233, 8 },
        { 5573, 8 },
        { 5764, 10 },
        { 4241, 8 },
        { 1729, 10 },
        { 27680, 18 },
        { 4930, 6 },
        { 11845, 4 },
        { 4497, 10 },
        { 4499, 12 },
        { 21843, 18 },
        { 33117, 18 },
        { 6756, 6 },
        { 3914, 14 },
        { 5080, 6 },
        { 804, 10 },
        { 5576, 10 },
        { 5575, 10 },
        { 1725, 12 },
        { 6754, 8 },
        { 857, 10 },
        { 933, 10 },
        { 3762, 12 },
        { 4238, 6 },
        { 10050, 12 },
        { 22976, 4 },
        { 14155, 16 },
        { 1470, 10 },
        { 21841, 16 },
        { 23852, 8 },
        { 1537, 8 },
        { 4957, 6 },
        { 17966, 18 },
        { 3352, 10 },
        { 19914, 18 },
        { 34845, 20 },
        { 21876, 20 },
        { 20400, 16 },
        { 2657, 8 },
        { 5762, 6 },
        { 10051, 12 },
        { 5763, 8 },
        { 14046, 14 },
        { 5571, 6 },
        { 828, 6 },
        { 4496, 6 },
        { 5572, 6 },
        { 805, 6 },
        { 4245, 10 },
        { 6446, 10 },
        { 1652, 12 },
        { 35516, 20 },
        { 20474, 4 },
        { 22679, 18 },
        { 34067, 20 },
        { 9587, 14 },
        { 4500, 16 },
        { 16885, 14 },
        { 11742, 16 },
        { 5574, 8 },
        { 2082, 6 },
        { 5603, 8 },
        { 4240, 8 }
    };

    public static Dictionary<uint, int> TBCQuivers { get; } = new Dictionary<uint, int>()
    {
        { 18714, 18 },
        { 29143, 18 },
        { 19319, 16 },
        { 7371, 14 },
        { 3573, 10 },
        { 34100, 20 },
        { 7278, 8 },
        { 2101, 6 },
        { 11362, 10 },
        { 8217, 16 },
        { 34105, 24 },
        { 3605, 12 },
        { 2662, 16 },
        { 5439, 8 },
        { 29144, 18 },
    };

    public static Dictionary<uint, int> TBCAmmoPouches { get; } = new Dictionary<uint, int>()
    {
        { 3604, 12 },
        { 19320, 16 },
        { 7372, 14 },
        { 3574, 10 },
        { 34099, 20 },
        { 11363, 10 },
        { 34106, 24 },
        { 2663, 16 },
        { 2102, 6 },
        { 7279, 8 },
        { 5441, 8 },
        { 29118, 18 },
        { 8218, 16 }
    };
}
