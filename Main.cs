using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using wManager.Plugin;
using wManager.Wow.Helpers;

public class Main : IPlugin
{
    public static string PluginName = "Wholesome Inventory Manager";
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();

    public static Dictionary<string, bool> WantedItemType = new Dictionary<string, bool>();

    public static string version = "0.0.02"; // Must match version in Version.txt

    public void Initialize()
    {
        isLaunched = true;

        AutoEquipSettings.Load();
        LoadWantedItemTypesList();

        if (AutoUpdater.CheckUpdate(version))
        {
            Logger.Log("New version downloaded, restarting, please wait");
            ToolBox.Restart();
            return;
        }

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();
        /*
        foreach (var ev in new string[] { "AUTOEQUIP_BIND_CONFIRM", "EQUIP_BIND_CONFIRM", "LOOT_BIND_CONFIRM", "USE_BIND_CONFIRM" })
        {
            EventsLua.AttachEventLua(ev, ctx => Lua.LuaDoString($"ConfirmBindOnUse()"));
        }
        */
        Setup();
    }

    public void Dispose()
    {
        detectionPulse.DoWork -= BackGroundPulse;
        detectionPulse.Dispose();
        Logger.Log("Disposed");
        isLaunched = false;
    }

    private void BackGroundPulse(object sender, DoWorkEventArgs args)
    {
        while (isLaunched)
        {
            try
            {
                Logger.LogDebug("--------------------------------------");
                DateTime dateBegin = DateTime.Now;

                WAECharacterSheet.Scan();
                WAEBagInventory.Scan();
                if (AutoEquipSettings.CurrentSettings.AutoEquipBags)
                    WAEBagInventory.BagEquip();
                if (AutoEquipSettings.CurrentSettings.AutoEquipGear)
                    WAECharacterSheet.AutoEquip();

                Logger.LogDebug($"Total Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
            }
            catch (Exception arg)
            {
                Logger.LogError(string.Concat(arg));
            }
            Thread.Sleep(5000);
        }
    }

    public void Settings()
    {
        AutoEquipSettings.Load();
        AutoEquipSettings.CurrentSettings.ShowConfiguration();
        AutoEquipSettings.CurrentSettings.Save();
    }

    private void LoadWantedItemTypesList()
    {
        WantedItemType.Clear();
        WantedItemType.Add("Bows", AutoEquipSettings.CurrentSettings.EquipBows);
        WantedItemType.Add("Crossbows", AutoEquipSettings.CurrentSettings.EquipCrossbows);
        WantedItemType.Add("Guns", AutoEquipSettings.CurrentSettings.EquipGuns);
        WantedItemType.Add("Thrown", AutoEquipSettings.CurrentSettings.EquipThrown);
    }

    private void Setup()
    {
        // Create invisible tooltip to read tooltip info
        Lua.LuaDoString($@"
        local tip = myTooltip or CreateFrame(""GAMETOOLTIP"", ""WEquipTooltip"")
        local L = L or tip: CreateFontString()
        local R = R or tip: CreateFontString()
        L: SetFontObject(GameFontNormal)
        R: SetFontObject(GameFontNormal)
        WEquipTooltip: AddFontStrings(L, R)
        WEquipTooltip: SetOwner(WorldFrame, ""ANCHOR_NONE"")");

        // Create function to read invisible tooltip lines
        Lua.LuaDoString($@"
        function EnumerateTooltipLines(...)
            local result = """"
            for i = 1, select(""#"", ...) do
                local region = select(i, ...)
                if region and region:GetObjectType() == ""FontString"" then
                    local text = region:GetText() or """"
                    if text ~= """" then
                        result = result .. ""|"" .. text
                    end
                end
            end
            return result
        end");
    }
}