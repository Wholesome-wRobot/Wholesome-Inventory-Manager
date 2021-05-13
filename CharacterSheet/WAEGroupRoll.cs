using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.CharacterSheet
{
    public class WAEGroupRoll
    {
        public static List<int> RollList = new List<int>();

        public static void CheckLootRoll()
        {
            DateTime dateBegin = DateTime.Now;

            for (int i = RollList.Count - 1; i >= 0; i--)
            {
                int rollId = RollList[i];

                bool canNeed = Lua.LuaDoString<bool>($"_, _, _, _, _, canNeed, _, _, _, _, _, _ = GetLootRollItemInfo({rollId});", "canNeed") || Main.WoWVersion <= ToolBox.WoWVersion.TBC;
                string itemLink = Lua.LuaDoString<string>($"itemLink = GetLootRollItemLink({rollId});", "itemLink");

                if (itemLink.Length < 10)
                {
                    Logger.LogDebug($"Couldn't get item link of roll {rollId}, skipping");
                    RollList.Remove(rollId);
                    continue;
                }

                WAEItem itemToRoll = new WAEItem(itemLink, rollId: rollId);

                if (AutoEquipSettings.CurrentSettings.AlwaysPass)
                {
                    Roll(rollId, itemToRoll, "Always pass", RollType.PASS);
                    RollList.Remove(rollId);
                    continue;
                }

                if (AutoEquipSettings.CurrentSettings.AlwaysGreed)
                {
                    Roll(rollId, itemToRoll, "Always greed", RollType.GREED);
                    RollList.Remove(rollId);
                    continue;
                }

                WAECharacterSheet.Scan();
                WAEContainers.Scan();
                WAEContainers.AllItems.Add(itemToRoll);

                if (canNeed && itemToRoll.ItemEquipLoc != "" && itemToRoll.ItemSubType != "Bag")
                {
                    // Weapons
                    if (WAEEnums.TwoHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || WAEEnums.OneHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || itemToRoll.ItemSubType == "Miscellaneous")
                        WAECharacterSheet.AutoEquipWeapons();

                    // Ranged
                    if (WAECharacterSheet.Ranged.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                        WAECharacterSheet.AutoEquipRanged();

                    // Trinket
                    if (itemToRoll.ItemEquipLoc == "INVTYPE_TRINKET")
                        WAECharacterSheet.AutoEquipTrinkets();

                    // Ring
                    if (itemToRoll.ItemEquipLoc == "INVTYPE_FINGER")
                        WAECharacterSheet.AutoEquipRings();

                    // Armor
                    foreach (WAECharacterSheetSlot armorSlot in WAECharacterSheet.ArmorSlots)
                    {
                        if (armorSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                        {
                            WAECharacterSheet.AutoEquipArmor();
                            break;
                        }
                    }
                }

                if (!itemToRoll.HasBeenRolled)
                    Roll(rollId, itemToRoll, "", RollType.GREED);

                RollList.Remove(rollId);
            }

            Logger.LogPerformance($"Loot Roll Check Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
        }

        public static void Roll(int rollId, WAEItem itemToRoll, string reason, RollType rollType)
        {
            int waitTime = 1000 + new Random().Next(1, 3000);
            string adjustedReason = reason == "" ? "" : $"[{reason}]";
 
            if (rollType == RollType.PASS)
            {
                Logger.Log($"Rolling PASS in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 0)");
            }

            if (rollType == RollType.GREED)
            {
                Logger.Log($"Rolling GREED in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 2)");
            }

            if (rollType == RollType.NEED)
            {
                Logger.Log($"Rolling NEED in {waitTime}ms for {itemToRoll.Name} ({itemToRoll.WeightScore}) {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 1)");
            }
        }
    }

    public enum RollType
    {
        PASS,
        GREED,
        NEED
    }
}
