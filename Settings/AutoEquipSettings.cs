using System;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Wholesome_Inventory_Manager.Settings;
using System.Collections.Generic;
using static WAEEnums;

[Serializable]
public class AutoEquipSettings : robotManager.Helpful.Settings
{
    public static AutoEquipSettings CurrentSettings { get; set; }
    public bool FirstLaunch { get; set; }
    public List<StatWeight> StatWeights { get; set; }
    public ClassSpec SpecSelectedByUser { get; set; }

    // Gear
    public bool AutoEquipGear { get; set; }
    public bool AutoDetectStatWeights { get; set; }
    // Weapons
    public bool EquipOneHanders { get; set; }
    public bool EquipTwoHanders { get; set; }
    public bool EquipShields { get; set; }
    // Ranged
    public bool EquipThrown { get; set; }
    public bool EquipBows { get; set; }
    public bool EquipCrossbows { get; set; }
    public bool EquipGuns { get; set; }
    // Bags
    public bool AutoEquipBags { get; set; }
    public bool EquipQuiver { get; set; }

    public double LastUpdateDate { get; set; }

    public AutoEquipSettings()
    {
        FirstLaunch = true;
        AutoDetectStatWeights = true;

        // Bags
        EquipQuiver = false;
        AutoEquipBags = false;
        LastUpdateDate = 0;

        // Gear
        AutoEquipGear = true;
        EquipThrown = true;
        EquipBows = true;
        EquipCrossbows = true;
        EquipGuns = true;

        EquipOneHanders = true;
        EquipTwoHanders = true;
        EquipShields = false;

        StatWeights = new List<StatWeight>();
    }

    public void ShowConfiguration()
    {
        PluginSettingsControl settingsWindow = new PluginSettingsControl();
        settingsWindow.MaxWidth = 600;
        settingsWindow.MaxHeight = 820;
        settingsWindow.MinWidth = 600;
        settingsWindow.MinHeight = 820;
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

    public static bool Load()
    {
        try
        {
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