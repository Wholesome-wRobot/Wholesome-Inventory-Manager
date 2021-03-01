using System.Collections.Generic;
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
        { "Plate", SkillLine.PlateMail }
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

    public static List<SkillLine> TwoHanders { get; set; } = new List<SkillLine>()
    {
        SkillLine.TwoHandedAxes,
        SkillLine.TwoHandedMaces,
        SkillLine.TwoHandedSwords,
        SkillLine.Staves,
        SkillLine.Polearms
    };
}
