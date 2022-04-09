using robotManager.Products;
using System.Threading;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using static WAEEnums;

public class ToolBox
{
    public static bool ImACaster()
    {
        return WAECharacterSheet.ClassSpec == ClassSpec.DruidBalance
            || WAECharacterSheet.ClassSpec == ClassSpec.DruidRestoration
            || WAECharacterSheet.ClassSpec == ClassSpec.MageArcane
            || WAECharacterSheet.ClassSpec == ClassSpec.MageFire
            || WAECharacterSheet.ClassSpec == ClassSpec.MageFrost
            || WAECharacterSheet.ClassSpec == ClassSpec.PaladinHoly
            || WAECharacterSheet.ClassSpec == ClassSpec.PriestDiscipline
            || WAECharacterSheet.ClassSpec == ClassSpec.PriestHoly
            || WAECharacterSheet.ClassSpec == ClassSpec.PriestShadow
            || WAECharacterSheet.ClassSpec == ClassSpec.ShamanElemental
            || WAECharacterSheet.ClassSpec == ClassSpec.ShamanRestoration
            || WAECharacterSheet.ClassSpec == ClassSpec.WarlockAffliction
            || WAECharacterSheet.ClassSpec == ClassSpec.WarlockDemonology
            || WAECharacterSheet.ClassSpec == ClassSpec.WarlockDestruction;
    }

    public static void Restart()
    {
        new Thread(() =>
        {
            Products.ProductStop();
            ToolBox.Sleep(2000);
            Products.ProductStart();
        }).Start();
    }

    public static bool WEEquipToolTipExists()
    {
        return !Lua.LuaDoString<bool>("return WEquipTooltip == nil;");
    }

    public static WoWVersion GetWoWVersion()
    {
        string version = WTLua.GetWoWVersion;
        if (version == "2.4.3")
            return WoWVersion.TBC;
        else
            return WoWVersion.WOTLK;
    }

    public static void Sleep(int milliseconds)
    {
        int worldLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagWorld");
        int homeLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagHome");
        Thread.Sleep(worldLatency + homeLatency + milliseconds);
    }

    public enum WoWVersion
    {
        VANILLA,
        TBC,
        WOTLK
    }
}