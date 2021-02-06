using System;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using MarsSettingsGUI;
using System.Configuration;
using System.ComponentModel;

[Serializable]
public class AutoEquipSettings : robotManager.Helpful.Settings
{
    SettingsWindow settingWindow;
    public static AutoEquipSettings CurrentSettings { get; set; }

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