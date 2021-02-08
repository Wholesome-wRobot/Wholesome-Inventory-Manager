using System;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using MarsSettingsGUI;
using System.Configuration;
using System.ComponentModel;
using System.Collections.Generic;

[Serializable]
public class AutoEquipSettings : robotManager.Helpful.Settings
{
    SettingsWindow settingWindow;
    public static AutoEquipSettings CurrentSettings { get; set; }

    // Stats Weight
    [Setting]
    [DefaultValue(true)]
    [Category("StatsWeight")]
    [DisplayName("Auto Detect")]
    [Description("Auto detect stats for your class and specialization. If this setting is enabled after closing this window, stats weights will be reset to default.")]
    public bool AutoDetect { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Stamina")]
    [Description("Stamina Weight")]
    public int StaminaWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Intellect")]
    [Description("Intellect Weight")]
    public int IntellectWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Agility")]
    [Description("Agility Weight")]
    public int AgilityWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Strength")]
    [Description("Strength Weight")]
    public int StrengthWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Spirit")]
    [Description("Spirit Weight")]
    public int SpiritWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Armor")]
    [Description("Armor Weight")]
    public int ArmorWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Attack Power")]
    [Description("Attack Power Weight")]
    public int AttackPowerWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Feral Attack Power")]
    [Description("Feral Attack Power Weight")]
    public int FeralAttackPower { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Spell Power")]
    [Description("Spell Power Weight")]
    public int SpellPowerWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Spell Penetration")]
    [Description("Spell Penetration Weight")]
    public int SpellPenetrationWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Mana per 5s")]
    [Description("Mana per 5s Weight")]
    public int Mana5Weight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Hit Rating")]
    [Description("Hit Rating Weight")]
    public int HitRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("DPS")]
    [Description("DPS Weight")]
    public int DPSWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Expertise Rating")]
    [Description("Expertise Rating Weight")]
    public int ExpertiseRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Crit Rating")]
    [Description("Crit Rating Weight")]
    public int CritRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Haste Rating")]
    [Description("Haste Rating Weight")]
    public int HasteRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Armor Penetration")]
    [Description("Armor Penetration Weight")]
    public int ArmorPenetrationWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Resilience Rating")]
    [Description("Resilience Rating Weight")]
    public int ResilienceWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Shield Block Rating")]
    [Description("Shield Block Rating Weight")]
    public int ShieldBlockRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Shield Block")]
    [Description("Shield Block Weight")]
    public int ShieldBlockWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Defense Rating")]
    [Description("Defense Rating Weight")]
    public int DefenseRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Parry Rating")]
    [Description("Parry Rating Weight")]
    public int ParryRatingWeight { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("StatsWeight")]
    [DisplayName("Dodge Rating")]
    [Description("Dodge Rating Weight")]
    public int DodgeRatingWeight { get; set; }

    // Bags
    [Setting]
    [DefaultValue(true)]
    [Category("Bags")]
    [DisplayName("Equip Bags")]
    [Description("Enable this setting to let the plugin handle bag equipment and upgrades")]
    public bool AutoEquipBags { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Bags")]
    [DisplayName("Ammo Container")]
    [Description("Enable this setting to let the plugin handle quiver equipment and upgrades for the Hunter. If disabled, you quiver will be replaced by a bag.")]
    public bool AutoEquipQuiver { get; set; }

    // Gear
    [Setting]
    [DefaultValue(true)]
    [Category("Gear")]
    [DisplayName("Equip Gear")]
    [Description("Enable this setting to let the plugin handle gear equipment")]
    public bool AutoEquipGear { get; set; }

    public double LastUpdateDate { get; set; }

    public AutoEquipSettings()
    {
        // Bags
        AutoEquipQuiver = true;
        AutoEquipBags = true;
        LastUpdateDate = 0;

        // Gear
        AutoEquipGear = true;

        // Stats Weights
        AutoDetect = true;
        ArmorWeight = 0;
        StaminaWeight = 0;
        IntellectWeight = 0;
        StrengthWeight = 0;
        SpiritWeight = 0;
        AttackPowerWeight = 0;
        SpellPowerWeight = 0;
        SpellPenetrationWeight = 0;
        DPSWeight = 0;
        HitRatingWeight = 0;
        ExpertiseRatingWeight = 0;
        Mana5Weight = 0;
        CritRatingWeight = 0;
        HasteRatingWeight = 0;
        ArmorPenetrationWeight = 0;
        ShieldBlockRatingWeight = 0;
        ShieldBlockWeight = 0;
        DefenseRatingWeight = 0;
        ParryRatingWeight = 0;
        DodgeRatingWeight = 0;
        AgilityWeight = 0;
        ResilienceWeight = 0;
    }

    public void ShowConfiguration()
    {
        settingWindow = new SettingsWindow(this, ObjectManager.Me.WowClass.ToString());
        settingWindow.MaxWidth = 800;
        settingWindow.MaxHeight = 800;
        settingWindow.MinWidth = 400;
        settingWindow.MinHeight = 400;
        settingWindow.SaveWindowPosition = true;
        settingWindow.Title = Main.PluginName + " Settings";
        settingWindow.ShowDialog();
    }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("AutoEquipSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logger.LogError("AutoEquipSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("AutoEquipSettings",
                ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<AutoEquipSettings>(
                    AdviserFilePathAndName("AutoEquipSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSettings = new AutoEquipSettings();
        }
        catch (Exception e)
        {
            Logger.LogError("AutoEquipSettings > Load(): " + e);
        }
        return false;
    }
}