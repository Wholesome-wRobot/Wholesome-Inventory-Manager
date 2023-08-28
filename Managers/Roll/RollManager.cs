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
        private readonly IClassSpecManager _classSpecManager;
        private readonly Random _rand = new Random();
        private CancellationTokenSource _rollLoopToken;
        private readonly List<Roll> _alreadyRolled = new List<Roll>();

        public RollManager(
            IEquipManager equipManager,
            ICharacterSheetManager characterSheetManager,
            ILootFilter lootFilter,
            IClassSpecManager classSpecManager)
        {
            _equipManager = equipManager;
            _characterSheetManager = characterSheetManager;
            _lootFilter = lootFilter;
            _classSpecManager = classSpecManager;
        }

        public void Initialize()
        {
            _rollLoopToken = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                while (!_rollLoopToken.IsCancellationRequested)
                {
                    _alreadyRolled.RemoveAll(roll => roll.ShouldBeRemoved);

                    string[] itemsToRoll = Lua.LuaDoString<string[]>($@"
                        local result = {{}};
                        for i=1,6 do
                            local lootIcon = _G['GroupLootFrame' .. i .. 'IconFrame'];
                            if lootIcon ~= nil and lootIcon:IsVisible() then
                                local rollId = lootIcon:GetParent().rollID;
                                local itemLink = GetLootRollItemLink(rollId);
                                table.insert(result, rollId .. '$' .. itemLink);
                            end
                        end
                        return unpack(result);
                    ");

                    if (itemsToRoll.Length > 0 && !string.IsNullOrEmpty(itemsToRoll[0]))
                    {
                        foreach (string item in itemsToRoll)
                        {
                            string[] details = item.Split('$');
                            int rollId = int.Parse(details[0]);
                            string itemLink = details[1];

                            if (!_alreadyRolled.Exists(roll => roll.RollId == rollId))
                            {
                                _alreadyRolled.Add(new Roll(rollId));
                                CheckLootRoll(rollId, itemLink);
                            }
                        }
                    }

                    Thread.Sleep(5000);
                }
            }, _rollLoopToken.Token);
        }

        public void Dispose()
        {
            _rollLoopToken?.Cancel();
        }

        private void CheckLootRoll(int rollId, string itemLink)
        {
            bool canNeed = Main.WoWVersion <= ToolBox.WoWVersion.TBC || Lua.LuaDoString<bool>($"local _, _, _, _, _, canNeed = GetLootRollItemInfo({rollId}); return canNeed;");

            if (string.IsNullOrEmpty(itemLink) || itemLink.Length < 10)
            {
                Logger.LogError($"Couldn't get item link of roll {rollId}, skipping");
                return;
            }

            IWIMItem itemToRoll = new WIMItem(itemLink, rollId: rollId);

            if (AutoEquipSettings.CurrentSettings.AlwaysPass)
            {
                DoRoll(rollId, itemToRoll, "Always pass", RollType.PASS);
                return;
            }

            if (AutoEquipSettings.CurrentSettings.AlwaysGreed)
            {
                DoRoll(rollId, itemToRoll, "Always greed", RollType.GREED);
                return;
            }

            if (!WAEEnums.ItemSkillsDictionary.ContainsKey(itemToRoll.ItemSubType))
            {
                DoRoll(rollId, itemToRoll, $"Irrelevant ItemSubType: {itemToRoll.ItemSubType}", RollType.GREED);
                return;
            }

            if (canNeed && itemToRoll.ItemEquipLoc != "" && itemToRoll.ItemSubType != "Bag")
            {
                // Weapons
                if (WAEEnums.TwoHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || WAEEnums.OneHanders.Contains(WAEEnums.ItemSkillsDictionary[itemToRoll.ItemSubType])
                    || (itemToRoll.ItemSubType == "Miscellaneous" && _classSpecManager.IAmCaster))
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsWeaponBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        DoRoll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ranged
                if (_characterSheetManager.RangedSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                {
                    string reason = _equipManager.IsRangedBetter(itemToRoll, true);
                    if (reason != null)
                    {
                        DoRoll(rollId, itemToRoll, reason, RollType.NEED);
                        return;
                    }
                }

                // Trinket
                if (itemToRoll.ItemEquipLoc == "INVTYPE_TRINKET")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsTrinketBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        DoRoll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ring
                if (itemToRoll.ItemEquipLoc == "INVTYPE_FINGER")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsRingBetter(itemToRoll, true);
                    if (slotAndReason != (null, null))
                    {
                        DoRoll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
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
                            DoRoll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                            return;
                        }
                    }
                }
            }

            if (!itemToRoll.HasBeenRolled)
            {
                DoRoll(rollId, itemToRoll, "", RollType.GREED);
            }
        }

        private void DoRoll(int rollId, IWIMItem itemToRoll, string reason, RollType rollType)
        {
            string adjustedReason = reason == "" ? "" : $"[{reason}]";
            int waitTime = 500;

            switch (rollType)
            {
                case RollType.PASS:
                    waitTime += _rand.Next(1, 1000);
                    Logger.Log($"Rolling PASS in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 0)"));
                    break;
                case RollType.GREED:
                    waitTime += _rand.Next(1, 2000);
                    Logger.Log($"Rolling GREED in {waitTime}ms for {itemToRoll.Name} {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 2)"));
                    break;
                case RollType.NEED:
                    waitTime += 500 + _rand.Next(1, 3000);
                    _lootFilter.ProtectFromFilter(itemToRoll.ItemLink);
                    Logger.Log($"Rolling NEED in {waitTime}ms for {itemToRoll.Name} ({itemToRoll.WeightScore}) {adjustedReason}");
                    Task.Delay(waitTime).ContinueWith(t => Lua.LuaDoString($"ConfirmLootRoll({rollId}, 1)"));
                    break;
            }

            itemToRoll.HasBeenRolled = true;
        }

        private struct Roll
        {
            private readonly Timer _timer;
            public int RollId { get; private set; }
            public bool ShouldBeRemoved => _timer.IsReady;
            public Roll(int rollId)
            {
                RollId = rollId;
                _timer = new Timer(1000 * 60);
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
