using System;
using System.Collections.Generic;
using System.ComponentModel;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

public class Main : IPlugin
{
    public static string PluginName = "Wholesome Inventory Manager";
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();

    public static Dictionary<string, bool> WantedItemType = new Dictionary<string, bool>();

    public static string version = "1.0.01"; // Must match version in Version.txt

    public void Initialize()
    {
        isLaunched = true;

        AutoEquipSettings.Load();

        if (AutoUpdater.CheckUpdate(version))
        {
            Logger.Log("New version downloaded, restarting, please wait");
            ToolBox.Restart();
            return;
        }

        Logger.Log($"Launching version {version} on client {ToolBox.GetWoWVersion()}");

        AutoDetectMyClassSpec();
        LoadWantedItemTypesList();
        WAECharacterSheet.RecordKnownSkills();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

        EventsLua.AttachEventLua("CHARACTER_POINTS_CHANGED", e => AutoDetectMyClassSpec());
        EventsLua.AttachEventLua("SKILL_LINES_CHANGED", e => WAECharacterSheet.RecordKnownSkills());
        EventsLua.AttachEventLua("QUEST_COMPLETE", e => WAEQuest.QuestRewardGossipOpen = true);
        wManager.Events.OthersEvents.OnSelectQuestRewardItem += WAEQuest.SelectReward;
        /*
        foreach (var ev in new string[] { "AUTOEQUIP_BIND_CONFIRM", "EQUIP_BIND_CONFIRM", "LOOT_BIND_CONFIRM", "USE_BIND_CONFIRM" })
        {
            EventsLua.AttachEventLua(ev, ctx => Lua.LuaDoString($"ConfirmBindOnUse()"));
        }
        */
        LUASetup();
    }

    public void Dispose()
    {
        detectionPulse.DoWork -= BackGroundPulse;
        wManager.Events.OthersEvents.OnSelectQuestRewardItem -= WAEQuest.SelectReward;
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
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause
                    && ObjectManager.Me.IsAlive
                    && !WAEQuest.SelectingReward)
                {
                    //Logger.LogDebug("--------------------------------------");
                    DateTime dateBegin = DateTime.Now;

                    WAECharacterSheet.Scan();
                    WAEContainers.Scan();

                    if (AutoEquipSettings.CurrentSettings.AutoEquipBags)
                        WAEContainers.BagEquip();

                    if (!ObjectManager.Me.InCombatFlagOnly && AutoEquipSettings.CurrentSettings.AutoEquipGear)
                        WAECharacterSheet.AutoEquip();

                    WAELootFilter.FilterLoot();

                    //Logger.LogDebug($"Total Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
                }
            }
            catch (Exception arg)
            {
                Logger.LogError(string.Concat(arg));
            }
            ToolBox.Sleep(5000);
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

    private void LUASetup()
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

    public static void AutoDetectMyClassSpec()
    {
        ClassSpec currentSpec = WAECharacterSheet.ClassSpec;

        switch (ObjectManager.Me.WowClass)
        {
            case (WoWClass.Warlock):
                if (ToolBox.GetSpec() == 2)
                    WAECharacterSheet.ClassSpec = ClassSpec.WarlockDemonology;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.WarlockDestruction;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.WarlockAffliction;
                break;

            case (WoWClass.DeathKnight):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.DeathKnightBloodDPS;
                else if (ToolBox.GetSpec() == 2)
                    WAECharacterSheet.ClassSpec = ClassSpec.DeathKnightFrostDPS;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.DeathKnightUnholy;
                break;

            case (WoWClass.Druid):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.DruidBalance;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.DruidRestoration;
                else
                {
                    if (ToolBox.GetTalentRank(2, 5) > 2 // Thick Hide
                        || ToolBox.GetTalentRank(2, 16) > 0 // Natural Reaction
                        || ToolBox.GetTalentRank(2, 22) > 0) // Protector of the Pack
                        WAECharacterSheet.ClassSpec = ClassSpec.DruidFeralTank;
                    else
                        WAECharacterSheet.ClassSpec = ClassSpec.DruidFeralDPS;
                }
                break;

            case (WoWClass.Hunter):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.HunterBeastMastery;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.HunterSurvival;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.HunterMarksman;
                break;

            case (WoWClass.Mage):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.MageArcane;
                else if (ToolBox.GetSpec() == 2)
                    WAECharacterSheet.ClassSpec = ClassSpec.MageFire;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.MageFrost;
                break;

            case (WoWClass.Paladin):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.PaladinHoly;
                else if (ToolBox.GetSpec() == 2)
                    WAECharacterSheet.ClassSpec = ClassSpec.PaladinProtection;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.PaladinRetribution;
                break;

            case (WoWClass.Priest):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.PriestDiscipline;
                else if (ToolBox.GetSpec() == 2)
                    WAECharacterSheet.ClassSpec = ClassSpec.PriestHoly;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.PriestShadow;
                break;

            case (WoWClass.Rogue):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.RogueAssassination;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.RogueSubtelty;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.RogueCombat;
                break;

            case (WoWClass.Shaman):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.ShamanElemental;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.ShamanRestoration;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.ShamanEnhancement;
                break;

            case (WoWClass.Warrior):
                if (ToolBox.GetSpec() == 1)
                    WAECharacterSheet.ClassSpec = ClassSpec.WarriorArms;
                else if (ToolBox.GetSpec() == 3)
                    WAECharacterSheet.ClassSpec = ClassSpec.WarriorTank;
                else
                    WAECharacterSheet.ClassSpec = ClassSpec.WarriorFury;
                break;

            default:
                WAECharacterSheet.ClassSpec = ClassSpec.None;
                break;
        }
        
        // Update stat weights in case of auto detect
        if (AutoEquipSettings.CurrentSettings.AutoDetectStatWeights && currentSpec != WAECharacterSheet.ClassSpec)
        {
            WAEItemDB.ItemDb.Clear(); // to Rescan all items
            SettingsPresets.ChangeStatsWeightSettings(WAECharacterSheet.ClassSpec);
        }
        
        // Set other default plugin settings according to detected class for first launch
        if (AutoEquipSettings.CurrentSettings.FirstLaunch && currentSpec != WAECharacterSheet.ClassSpec)
        {
            Logger.Log("First Launch");
            SettingsPresets.ChangeAutoEquipSetting(WAECharacterSheet.ClassSpec);
            AutoEquipSettings.CurrentSettings.FirstLaunch = false;
            AutoEquipSettings.CurrentSettings.Save();
        }

        AutoEquipSettings.CurrentSettings.SpecSelectedByUser = WAECharacterSheet.ClassSpec;
    }
}