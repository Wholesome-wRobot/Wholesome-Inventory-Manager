using System;
using System.ComponentModel;
using System.Threading;
using wManager.Plugin;
using wManager.Wow.Helpers;

public class Main : IPlugin
{
    public static string PluginName = "Wholesome Inventory Manager";
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();

    public void Initialize()
    {
        isLaunched = true;

        AutoEquipSettings.Load();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

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
                Logger.Log("--------------------------------------");
                DateTime dateBegin = DateTime.Now;

                WAECharacterSheet.Scan();
                WAEBagInventory.Scan();
                WAEBagInventory.BagEquip();

                Logger.Log($"Total Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
            }
            catch (Exception arg)
            {
                Logger.LogError(string.Concat(arg));
            }
            Thread.Sleep(3000);
        }
    }

    public void Settings()
    {
        AutoEquipSettings.Load();
        AutoEquipSettings.CurrentSettings.ShowConfiguration();
        AutoEquipSettings.CurrentSettings.Save();
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