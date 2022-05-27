using System;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Filter;
using Wholesome_Inventory_Manager.Managers.Items;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.Roll
{
    internal class RollManager : IRollManager
    {
        private readonly IEquipManager _equipManager;
        private readonly ICharacterSheetManager _characterSheetManager;
        private readonly ILootFilter _lootFilter;
        private readonly Random _rand = new Random();

        public RollManager(IEquipManager equipManager, ICharacterSheetManager characterSheetManager, ILootFilter lootFilter)
        {
            _equipManager = equipManager;
            _characterSheetManager = characterSheetManager;
            _lootFilter = lootFilter;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        public void CheckLootRoll(int rollId)
        {
            bool canNeed = Main.WoWVersion <= ToolBox.WoWVersion.TBC || Lua.LuaDoString<bool>($"local _, _, _, _, _, canNeed = GetLootRollItemInfo({rollId});", "canNeed");
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
                    (ISheetSlot, string) slotAndReason = _equipManager.IsWeaponBetter(itemToRoll);
                    if (slotAndReason != (null, null))
                    {
                        Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ranged
                if (_characterSheetManager.RangedSlot.InvTypes.Contains(itemToRoll.ItemEquipLoc))
                {
                    string reason = _equipManager.IsRangedBetter(itemToRoll);
                    if (reason != null)
                    {
                        Roll(rollId, itemToRoll, reason, RollType.NEED);
                        return;
                    }
                }

                // Trinket
                if (itemToRoll.ItemEquipLoc == "INVTYPE_TRINKET")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsTrinketBetter(itemToRoll);
                    if (slotAndReason != (null, null))
                    {
                        Roll(rollId, itemToRoll, slotAndReason.Item2, RollType.NEED);
                        return;
                    }
                }

                // Ring
                if (itemToRoll.ItemEquipLoc == "INVTYPE_FINGER")
                {
                    (ISheetSlot, string) slotAndReason = _equipManager.IsRingBetter(itemToRoll);
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
                        (ISheetSlot, string) slotAndReason = _equipManager.IsArmorBetter(armorSlot, itemToRoll);
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
