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
    private IClassSpecManager _classSpecManager;

    private bool _shouldBagUpdate = false;
    private Timer _bagUpdateTimer = new Timer(500);

    public void Initialize()
    {
        AutoEquipSettings.Load(_classSpecManager);

        if (AutoUpdater.CheckUpdate(version))
        {
            Logger.Log("New version downloaded, restarting, please wait");
            ToolBox.Restart();
            return;
        }

        Logger.Log($"Launching version {version} on client {WoWVersion}");

        ToolBox.LUASetup();

        _classSpecManager = new ClassSpecManager();
        _classSpecManager.Initialize();
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
        _rollManager = new RollManager(_equipManager, _characterSheetManager, _lootFilter, _classSpecManager);
        _rollManager.Initialize();
        _questRewardManager = new QuestRewardManager(_equipManager, _characterSheetManager);
        _questRewardManager.Initialize();        

        EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
    }

    public void Dispose()
    {
        EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;

        _classSpecManager?.Dispose();
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
            case "PLAYER_ENTERING_WORLD":
                ToolBox.LUASetup();
                break;
                /*
            case "START_LOOT_ROLL":
                _rollManager.CheckLootRoll(args);
                break;
                */
            case "PLAYER_EQUIPMENT_CHANGED":
                _characterSheetManager.Scan();
                break;
            case "CHARACTER_POINTS_CHANGED":
                _skillsManager.RecordSkills();
                _classSpecManager.AutoDetectSpec();
                break;
            case "SKILL_LINES_CHANGED":
                _skillsManager.RecordSkills();
                break;
            case "BAG_UPDATE":
            case "PLAYER_REGEN_ENABLED":
                _shouldBagUpdate = true;
                break;
            case "UNIT_INVENTORY_CHANGED":
                if (args[0] == "player")
                    _shouldBagUpdate = true;
                break;
        }

        if (_shouldBagUpdate && _bagUpdateTimer.IsReady)
        {
            _shouldBagUpdate = false;
            _equipManager.CheckAll();
            _bagUpdateTimer.Reset();
        }
    }

    public void Settings()
    {
        AutoEquipSettings.Load(_classSpecManager);
        AutoEquipSettings.CurrentSettings.ShowConfiguration();
        AutoEquipSettings.CurrentSettings.Save();
    }
}