using System;
using System.Collections.Generic;
using System.IO;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Settings;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

[Serializable]
public class AutoEquipSettings : robotManager.Helpful.Settings
{
    private static IClassSpecManager _classSpecManager;

    public static AutoEquipSettings CurrentSettings { get; set; }
    public bool FirstLaunch { get; set; }
    public List<StatWeight> StatWeights { get; set; }
    public ClassSpec SelectedSpec { get; set; }

    // Gear
    public bool AutoEquipGear { get; set; }
    public bool AutoSelectQuestRewards { get; set; }
    public bool AutoDetectStatWeights { get; set; }
    public bool SwitchRanged { get; set; }
    // Weapons
    public bool EquipOneHanders { get; set; }
    public bool EquipTwoHanders { get; set; }
    public bool EquipShields { get; set; }
    // Ranged
    public bool EquipThrown { get; set; }
    public bool EquipBows { get; set; }
    public bool EquipCrossbows { get; set; }
    public bool EquipGuns { get; set; }
    public bool EquipAmmo { get; set; }
    // Bags
    public bool AutoEquipBags { get; set; }
    public bool EquipQuiver { get; set; }

    // LootFilter
    public bool LootFilterActivated { get; set; }
    public bool DeleteDeprecatedQuestItems { get; set; }
    // Quality
    public bool DeleteGray { get; set; }
    public bool AnyGray { get; set; }
    public bool KeepGray { get; set; }
    public bool DeleteWhite { get; set; }
    public bool AnyWhite { get; set; }
    public bool KeepWhite { get; set; }
    public bool DeleteGreen { get; set; }
    public bool AnyGreen { get; set; }
    public bool KeepGreen { get; set; }
    public bool DeleteBlue { get; set; }
    public bool AnyBlue { get; set; }
    public bool KeepBlue { get; set; }
    // Value
    public int DeleteGoldValue { get; set; }
    public int DeleteSilverValue { get; set; }
    public int DeleteCopperValue { get; set; }
    public bool DeleteItemWithNoValue { get; set; }
    // Group roll
    public bool AlwaysGreed { get; set; }
    public bool AlwaysPass { get; set; }
    // MISC
    public bool RestackItems { get; set; }
    public bool UseScrolls { get; set; }

    public bool LogItemInfo { get; set; }

    public double LastUpdateDate { get; set; }

    internal AutoEquipSettings()
    {
        FirstLaunch = true;
        AutoDetectStatWeights = true;
        SelectedSpec = ClassSpec.None;
        LogItemInfo = false;

        // Filter Loot
        LootFilterActivated = false;

        DeleteDeprecatedQuestItems = false;

        DeleteGoldValue = 0;
        DeleteSilverValue = 0;
        DeleteCopperValue = 0;
        DeleteItemWithNoValue = false;

        DeleteGray = false;
        AnyGray = false;
        KeepGray = true;

        DeleteWhite = false;
        AnyWhite = false;
        KeepWhite = true;

        DeleteGreen = false;
        AnyGreen = false;
        KeepGreen = true;

        DeleteBlue = false;
        AnyBlue = false;
        KeepBlue = true;

        // Bags
        EquipQuiver = false;
        AutoEquipBags = true;
        LastUpdateDate = 0;

        // Gear
        AutoEquipGear = true;
        AutoSelectQuestRewards = true;
        SwitchRanged = false;
        EquipThrown = true;
        EquipBows = true;
        EquipCrossbows = true;
        EquipGuns = true;
        EquipAmmo = true;

        EquipOneHanders = true;
        EquipTwoHanders = true;
        EquipShields = true;

        // Group roll
        AlwaysGreed = false;
        AlwaysPass = false;

        RestackItems = true;
        UseScrolls = true;

        StatWeights = new List<StatWeight>();
    }

    public void ShowConfiguration()
    {
        PluginSettingsControl settingsWindow = new PluginSettingsControl(_classSpecManager);
        settingsWindow.MaxWidth = 520;
        settingsWindow.MaxHeight = 650;
        settingsWindow.MinWidth = 520;
        settingsWindow.MinHeight = 650;
        settingsWindow.ResizeMode = System.Windows.ResizeMode.CanResize;
        settingsWindow.Title = Main.PluginName;
        settingsWindow.SaveWindowPosition = true;
        settingsWindow.ShowDialog();
    }

    [Serializable]
    public struct StatWeight
    {
        public CharStat Name;
        public int Value;
        public StatWeight(CharStat name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public int GetStat(CharStat stat)
    {
        for (int i = 0; i < StatWeights.Count; i++)
        {
            if (StatWeights[i].Name == stat)
                return StatWeights[i].Value;
        }
        return 0;
    }

    public void SetStat(CharStat stat, int value)
    {
        int key = -1;
        for (int i = 0; i < StatWeights.Count; i++)
        {
            if (StatWeights[i].Name == stat)
            {
                key = i;
                break;
            }
        }

        if (key == -1)
            StatWeights.Add(new StatWeight(stat, value));
        else
            StatWeights[key] = new StatWeight(stat, value);
    }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("AutoEquipSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logger.LogError("AutoEquipSettings > Save(): " + e);
            return false;
        }
    }

    internal static bool Load(IClassSpecManager classSpecManager)
    {
        try
        {
            _classSpecManager = classSpecManager;
            if (File.Exists(AdviserFilePathAndName("AutoEquipSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSettings = Load<AutoEquipSettings>(AdviserFilePathAndName("AutoEquipSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
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