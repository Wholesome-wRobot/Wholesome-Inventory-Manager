using System;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class AutoEquipSettings : robotManager.Helpful.Settings
{
    public static AutoEquipSettings CurrentSettings { get; set; }

    private AutoEquipSettings()
    {
        LastUpdateDate = 0;
    }

    public double LastUpdateDate { get; set; }

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
