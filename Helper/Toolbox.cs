﻿using robotManager.Products;
using System.Threading;
using WholesomeToolbox;
using wManager.Wow.Helpers;

public class ToolBox
{
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

    public static void PrintLuaTime(string suffix)
    {
        Lua.LuaDoString($@"DEFAULT_CHAT_FRAME:AddMessage(""{suffix} "" .. date(""%H:%M:%S""));");
    }
}