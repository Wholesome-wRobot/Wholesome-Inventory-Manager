using robotManager.Products;
using System.Threading;
using WholesomeToolbox;
using wManager.Wow.Helpers;

public class ToolBox
{
    public static int ParseInt(string str)
    {
        if (str != null && int.TryParse(str, out int val))
        {
            return val;
        }
        else
        {
            Logger.LogError($"ERROR: Unable to parse {str}");
            return 0;
        }
    }

    public static void Restart()
    {
        new Thread(() =>
        {
            Products.ProductStop();
            Sleep(2000);
            Products.ProductStart();
        }).Start();
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
        int latency = Lua.LuaDoString<int>($@"
            local result = 0;
            local _, _, lagHome, lagWorld = GetNetStats();
            if lagHome ~= nil then result = result + lagHome end;
            if lagWorld ~= nil then result = result + lagWorld end;
            return result;
        ");
        Thread.Sleep(latency + milliseconds);
    }

    public enum WoWVersion
    {
        VANILLA,
        TBC,
        WOTLK
    }

    public static void PrintLuaTime(string suffix)
    {
        Lua.LuaDoString($@"DEFAULT_CHAT_FRAME:AddMessage(""{suffix} "" .. date(""%H:%M:%S""));");
    }
}