using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using Wholesome_Inventory_Manager.Managers.Bags;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Filter;
using Wholesome_Inventory_Manager.Managers.Items;
using Wholesome_Inventory_Manager.Managers.Quest;
using Wholesome_Inventory_Manager.Managers.Roll;
using wManager.Plugin;
using wManager.Wow.Helpers;

public class Main : IPlugin
{
    public static string PluginName = "Wholesome Inventory Manager";
    public static string version = FileVersionInfo.GetVersionInfo(Others.GetCurrentDirectory + @"\Plugins\Wholesome_Inventory_Manager.dll").FileVersion;

    public static ToolBox.WoWVersion WoWVersion = ToolBox.GetWoWVersion();

    private IEquipManager _equipManager;
    private IRollManager _rollManager;
    private ISkillsManager _skillsManager;
    private ICharacterSheetManager _characterSheetManager;
    private IWIMContainers _containers;
    private ILootFilter _lootFilter;
    private IQuestRewardManager _questRewardManager;

    public void Initialize()
    {
        LUASetup();

        AutoEquipSettings.Load();
        ClassSpecManager.Initialize();

        if (AutoUpdater.CheckUpdate(version))
        {
            Logger.Log("New version downloaded, restarting, please wait");
            ToolBox.Restart();
            return;
        }

        Logger.Log($"Launching version {version} on client {WoWVersion}");

        _skillsManager = new SkillsManager();
        _skillsManager.Initialize();
        _characterSheetManager = new CharacterSheetManager();
        _characterSheetManager.Initialize();
        _lootFilter = new LootFilter();
        _lootFilter.Initialize();
        _containers = new WIMContainers(_characterSheetManager, _lootFilter);
        _containers.Initialize();
        _equipManager = new EquipManager(_skillsManager, _characterSheetManager, _containers, _lootFilter);
        _equipManager.Initialize();
        _rollManager = new RollManager(_equipManager, _characterSheetManager, _lootFilter);
        _rollManager.Initialize();
        _questRewardManager = new QuestRewardManager(_equipManager, _characterSheetManager);
        _questRewardManager.Initialize();

        EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
    }

    public void Dispose()
    {
        EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;

        ClassSpecManager.Dispose();
        _questRewardManager?.Dispose();
        _rollManager?.Dispose();
        _equipManager?.Dispose();
        _containers?.Dispose();
        _lootFilter?.Dispose();
        _characterSheetManager?.Dispose();
        _skillsManager?.Dispose();
        Logger.Log("Disposed");
    }

    private void OnEventsLuaWithArgs(string id, List<string> args)
    {
        switch (id)
        {
            case "START_LOOT_ROLL":
                Logger.Log($"ROLL DETECTED");
                _rollManager.CheckLootRoll(int.Parse(args[0]));
                break;
            case "CHARACTER_POINTS_CHANGED":
                ClassSpecManager.DetectSpec();
                break;
            case "SKILL_LINES_CHANGED":
                _skillsManager.RecordSkills();
                break;
            case "UNIT_INVENTORY_CHANGED":
                _equipManager.CheckAll();
                break;
            case "PLAYER_EQUIPMENT_CHANGED":
                _characterSheetManager.Scan();
                break;
            case "BAG_UPDATE":
                _equipManager.CheckAll();
                break;
            case "PLAYER_ENTERING_WORLD":
                LUASetup();
                break;
            case "PLAYER_REGEN_ENABLED":
                _equipManager.CheckAll();
                break;
        }
    }

    public void Settings()
    {
        AutoEquipSettings.Load();
        AutoEquipSettings.CurrentSettings.ShowConfiguration();
        AutoEquipSettings.CurrentSettings.Save();
    }

    private void LUASetup()
    {
        if (Lua.LuaDoString<bool>("return WEquipTooltip ~= nil;"))
            return;

        // Create invisible tooltip to read tooltip info
        Lua.LuaDoString($@"
                local tip = WEquipTooltip or CreateFrame(""GAMETOOLTIP"", ""WEquipTooltip"")
                local L = L or tip: CreateFontString()
                local R = R or tip: CreateFontString()
                L: SetFontObject(GameFontNormal)
                R: SetFontObject(GameFontNormal)
                WEquipTooltip: AddFontStrings(L, R)
                WEquipTooltip: SetOwner(WorldFrame, ""ANCHOR_NONE"")"
            );

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
                end"
            );
    }
}