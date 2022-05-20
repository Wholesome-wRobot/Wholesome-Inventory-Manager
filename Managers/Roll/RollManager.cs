using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.Roll
{
    internal class RollManager : IRollManager
    {
        private List<int> _rollList = new List<int>();
        private readonly IEquipManager _equipManager;
        private readonly ICharacterSheetManager _characterSheetManager;
        private readonly object _rollLock = new object();

        public RollManager(IEquipManager equipManager, ICharacterSheetManager characterSheetManager)
        {
            _equipManager = equipManager;
            _characterSheetManager = characterSheetManager;
        }

        public void Initialize()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
        }

        private void OnEventsLuaWithArgs(string id, List<string> args)
        {
            switch (id)
            {
                case "START_LOOT_ROLL":
                    lock (_rollLock)
                    {
                        _rollList.Add(int.Parse(args[0]));
                    }
                    CheckLootRoll();
                    break;
            }
        }

        public void CheckLootRoll()
        {
            lock (_rollLock)
            {
                for (int i = _rollList.Count - 1; i >= 0; i--)
                {
                    int rollId = _rollList[i];

                    bool canNeed = Lua.LuaDoString<bool>($"_, _, _, _, _, canNeed, _, _, _, _, _, _ = GetLootRollItemInfo({rollId});", "canNeed") || Main.WoWVersion <= ToolBox.WoWVersion.TBC;
                    string itemLink = Lua.LuaDoString<string>($"itemLink = GetLootRollItemLink({rollId});", "itemLink");

                    if (itemLink.Length < 10)
                    {
                        Logger.LogDebug($"Couldn't get item link of roll {rollId}, skipping");
                        _rollList.Remove(rollId);
                        continue;
                    }

                    IWIMItem itemToRoll = new WIMItem(itemLink, rollId: rollId);

                    if (AutoEquipSettings.CurrentSettings.AlwaysPass)
                    {
                        Roll(rollId, itemToRoll, "Always pass", RollType.PASS);
                        _rollList.Remove(rollId);
                        continue;
                    }

                    if (AutoEquipSettings.CurrentSettings.AlwaysGreed)
                    {
                        Roll(rollId, itemToRoll, "Always greed", RollType.GREED);
                        _rollList.Remove(rollId);
                        continue;
                    }

                    if (canNeed && itemToRoll.ItemEquipLoc != "" && itemToRoll.ItemSubType != "Bag")
                    {
                        // Weapons
                        if (WAEEnums.TwoHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                            || WAEEnums.OneHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                            || (itemToRoll.ItemSubType == "Miscellaneous" && ClassSpecManager.ImACaster()))
                        {
                            (ISheetSlot, string) slotAndReason = _equipManager.IsWeaponBetter(itemToRoll);
                            if (slotAndReason != (null, null))
                                Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        }

                        // Ranged
                        if (_characterSheetManager.RangedSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                        {
                            string reason = _equipManager.IsRangedBetter(itemToRoll);
                            if (reason != null)
                                Roll(rollId, itemToRoll, reason, RollType.NEED);
                        }

                        // Trinket
                        if (itemToRoll.ItemEquipLoc == "INVTYPE_TRINKET")
                        {
                            (ISheetSlot, string) slotAndReason = _equipManager.IsTrinketBetter(itemToRoll);
                            if (slotAndReason != (null, null))
                                Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        }

                        // Ring
                        if (itemToRoll.ItemEquipLoc == "INVTYPE_FINGER")
                        {
                            (ISheetSlot, string) slotAndReason = _equipManager.IsRingBetter(itemToRoll);
                            if (slotAndReason != (null, null))
                                Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        }

                        // Armor
                        foreach (ISheetSlot armorSlot in _characterSheetManager.ArmorSlots)
                        {
                            if (armorSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                            {
                                (ISheetSlot, string) slotAndReason = _equipManager.IsArmorBetter(armorSlot, itemToRoll);
                                if (slotAndReason != (null, null))
                                    Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                            }
                        }
                    }

                    if (!itemToRoll.HasBeenRolled)
                    {
                        Roll(rollId, itemToRoll, "", RollType.GREED);
                    }

                    _rollList.Remove(rollId);
                }
            }
        }

        private void Roll(int rollId, IWIMItem itemToRoll, string reason, RollType rollType)
        {
            string adjustedReason = reason == "" ? "" : $"[{reason}]";

            if (rollType == RollType.PASS)
            {
                int waitTime = 500 + new Random().Next(1, 500);
                Logger.Log($"Rolling PASS in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 0)");
            }

            if (rollType == RollType.GREED)
            {
                int waitTime = 500 + new Random().Next(1, 1000);
                Logger.Log($"Rolling GREED in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 2)");
            }

            if (rollType == RollType.NEED)
            {
                int waitTime = 1000 + new Random().Next(1, 3000);
                Logger.Log($"Rolling NEED in {waitTime}ms for {itemToRoll.Name} ({itemToRoll.WeightScore}) {adjustedReason}");
                Thread.Sleep(waitTime);
                Lua.LuaDoString($"ConfirmLootRoll({rollId}, 1)");
            }
        }

        public enum RollType
        {
            PASS,
            GREED,
            NEED
        }
    }
}
