using robotManager.Products;
using System.Diagnostics;
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

    public static void LUASetup()
    {
        // Create function to read invisible tooltip lines
        Lua.LuaDoString($@"
                if (WEquipTooltip == nil) then
                    CreateFrame(""GAMETOOLTIP"", ""WEquipTooltip"");
                    local L = L or WEquipTooltip: CreateFontString();
                    local R = R or WEquipTooltip: CreateFontString();
                    L: SetFontObject(GameFontNormal);
                    R: SetFontObject(GameFontNormal);
                    WEquipTooltip: AddFontStrings(L, R);
                    WEquipTooltip: SetOwner(WorldFrame, ""ANCHOR_NONE"");
                end

                if (EnumerateTooltipLines == nil) then
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
                    end
                end

                if (ParseItemInfo == nil) then
                    function ParseItemInfo(i, j, paramItemLink)
                        WEquipTooltip:ClearLines();
                        WEquipTooltip:SetHyperlink(paramItemLink);
                        local itemName, itemLink, itemRarity, itemLevel, itemMinLevel, itemType,
                            itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(paramItemLink);
                        local texture, count, locked, quality, readable, lootable, link = GetContainerItemInfo(i, j);
                        if (count == null) then
                            count = 1;
                        end
                        if (itemSellPrice == null) then
                            itemSellPrice = 0;
                        end
                        if (itemEquipLoc == null) then
                            itemEquipLoc = '';
                        end      
                        return table.concat({{ i, j, itemLink, itemName, itemRarity, itemLevel, itemMinLevel, itemType,
                            itemSubType, itemStackCount, itemEquipLoc, itemSellPrice, count, EnumerateTooltipLines(WEquipTooltip: GetRegions()) }}, ""£"");
                    end
                end
            ");
    }
}