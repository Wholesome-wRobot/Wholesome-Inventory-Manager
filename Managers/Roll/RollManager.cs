using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Filter;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Helpers;
using Timer = robotManager.Helpful.Timer;

namespace Wholesome_Inventory_Manager.Managers.Roll
{
    internal class RollManager : IRollManager
    {
        private readonly IEquipManager _equipManager;
        private readonly ICharacterSheetManager _characterSheetManager;
        private readonly ILootFilter _lootFilter;
        private readonly Random _rand = new Random();
        private static readonly Timer _rollFailSafeCoolDown = new Timer(15 * 1000);
        private CancellationTokenSource _failSafeToken;

        public RollManager(IEquipManager equipManager, ICharacterSheetManager characterSheetManager, ILootFilter lootFilter)
        {
            _equipManager = equipManager;
            _characterSheetManager = characterSheetManager;
            _lootFilter = lootFilter;
        }

        public void Initialize()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
            _failSafeToken = new CancellationTokenSource();
            
            Task.Factory.StartNew(() =>
            {
                while (!_failSafeToken.IsCancellationRequested)
                {
                    if (_rollFailSafeCoolDown.IsReady)
                    {
                        // Any roll is available after 15s cooldown, meaning a roll failed (mostly because of lvl up)
                        // Greed it because we can't get the roll ID/item when the event is missed
                        bool safeRolled = Lua.LuaDoString<bool>($@"
                            local saferolled = false;
                            for i=1,10 do
                                local greedButton = _G['GroupLootFrame' .. i .. 'GreedButton'];
                                if greedButton ~= nil and greedButton:IsVisible() then
                                    greedButton:Click();
                                    StaticPopup1Button1:Click();
                                    saferolled = true;
                                end
                            end
                            return saferolled;
                        ");

                        if (safeRolled) 
                            Logger.LogError($"We missed a roll, defaulted to greed");
                    }
                    Thread.Sleep(5000);
                }
            }, _failSafeToken.Token);
        }

        public void Dispose()
        {
            _failSafeToken?.Cancel();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
        }

        private void OnEventsLuaWithArgs(string id, List<string> args)
        {
            if (id == "START_LOOT_ROLL")
            {
                _rollFailSafeCoolDown.Reset();
                if (int.TryParse(args[0], out int rollId))
                {
                    CheckLootRoll(rollId);
                }
                else
                {
                    Logger.LogError($"Couldn't parse roll ID!");
                }
            }
        }

        public void CheckLootRoll(int rollId)
        {
            bool canNeed = Main.WoWVersion <= ToolBox.WoWVersion.TBC || Lua.LuaDoString<bool>($"local _, _, _, _, _, canNeed = GetLootRollItemInfo({rollId}); return canNeed;");
            string itemLink = Lua.LuaDoString<string>($"return GetLootRollItemLink({rollId});");

            if (itemLink.Length < 10)
            {
                Logger.LogError($"Couldn't get item link of roll {rollId}, skipping");
                return;
            }

            IWIMItem itemToRoll = new WIMItem(itemLink, rollId: rollId);

            if (AutoEquipSettings.CurrentSettings.AlwaysPass)
            {
                Roll(rollId, itemToRoll, "Always pass", RollType.PASS);
                return;
            }

            if (AutoEquipSettings.CurrentSettings.AlwaysGreed)
            {
                Roll(rollId, itemToRoll, "Always greed", RollType.GREED);
                return;
            }

            if (canNeed && itemToRoll.ItemEquipLoc != "" && itemToRoll.ItemSubType != "Bag")
            {
                // Weapons
                if (WAEEnums.TwoHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || WAEEnums.OneHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || (itemToRoll.ItemSubType == "Miscellaneous" && ClassSpecManager.ImACaster()))
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsWeaponBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ranged
                if (_characterSheetManager.RangedSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                {
                    string reason = _equipManager.IsRangedBetter(itemToRoll, true);
                    if (reason != null)
                    {
                        Roll(rollId, itemToRoll, reason, RollType.NEED);
                        return;
                    }
                }

                // Trinket
                if (itemToRoll.ItemEquipLoc == "INVTYPE_TRINKET")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsTrinketBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ring
                if (itemToRoll.ItemEquipLoc == "INVTYPE_FINGER")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsRingBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Armor
                foreach (ISheetSlot armorSlot in _characterSheetManager.ArmorSlots)
                {
                    if (armorSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                    {
                        (ISheetSlot, string) slotAndReason = _equipManager.IsArmorBetter(armorSlot, itemToRoll, true);
                        if (slotAndReason != (null, null))
                        {
                            Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                            return;
                        }
                    }
                }
            }

            if (!itemToRoll.HasBeenRolled)
            {
                Roll(rollId, itemToRoll, "", RollType.GREED);
            }
        }

        private void Roll(int rollId, IWIMItem itemToRoll, string reason, RollType rollType)
        {
            string adjustedReason = reason == "" ? "" : $"[{reason}]";
            int waitTime = 2000;

            switch (rollType)
            {
                case RollType.PASS:
                    waitTime += _rand.Next(1, 2000);
                    Logger.Log($"Rolling PASS in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 0)"));

                    break;
                case RollType.GREED:
                    waitTime += _rand.Next(1, 4000);
                    Logger.Log($"Rolling GREED in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 2)"));
                    break;
                case RollType.NEED:
                    waitTime += 500 + _rand.Next(1, 6000);
                    _lootFilter.ProtectFromFilter(itemToRoll.ItemLink);
                    Logger.Log($"Rolling NEED in {waitTime}ms for {itemToRoll.Name} ({itemToRoll.WeightScore}) {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 1)"));
                    break;
            }

            itemToRoll.HasBeenRolled = true;
        }

        public enum RollType
        {
            PASS,
            GREED,
            NEED
        }
    }
}
