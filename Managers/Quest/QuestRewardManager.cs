using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using Wholesome_Inventory_Manager.Managers.Items;
using WholesomeToolbox;
using wManager.Events;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

namespace Wholesome_Inventory_Manager.Managers.Quest
{
    internal class QuestRewardManager : IQuestRewardManager
    {
        private readonly IEquipManager _equipManager;
        private readonly ICharacterSheetManager _characterSheetManager;

        public QuestRewardManager(IEquipManager equipManager, ICharacterSheetManager characterSheetManager)
        {
            _equipManager = equipManager;
            _characterSheetManager = characterSheetManager;
        }

        public void Initialize()
        {
            OthersEvents.OnSelectQuestRewardItem += SelectReward;
        }

        public void Dispose()
        {
            OthersEvents.OnSelectQuestRewardItem -= SelectReward;
        }

        public void SelectReward(CancelEventArgs cancelable)
        {
            if (!AutoEquipSettings.CurrentSettings.AutoSelectQuestRewards)
                return;

            int nbQuestRewards = WTGossip.NbQuestChoices;

            if (nbQuestRewards > 0 && QuestRewardFrameOpen)
            {
                cancelable.Cancel = true;

                List<IWIMItem> itemRewards = new List<IWIMItem>();
                for (int i = 1; i <= nbQuestRewards; i++)
                {
                    string itemLink = Lua.LuaDoString<string>($"return GetQuestItemLink(\"choice\", {i})");
                    itemRewards.Add(new WIMItem(itemLink, rewardSlot: i));
                }

                itemRewards = itemRewards.OrderByDescending(i => i.WeightScore).ToList();
                bool rewardPicked = false;

                foreach (IWIMItem item in itemRewards)
                {
                    if (item.ItemEquipLoc != "" && item.ItemSubType != "Bag")
                    {
                        // Weapons
                        if (WAEEnums.ItemSkillsDictionary.TryGetValue(item.ItemSubType, out SkillLine skillLine))
                        {
                            if (WAEEnums.TwoHanders.Contains(skillLine)
                                || WAEEnums.OneHanders.Contains(skillLine)
                                || item.ItemSubType == "Miscellaneous")
                            {
                                (ISheetSlot, string) slotAndReason = _equipManager.IsWeaponBetter(item);
                                if (slotAndReason != (null, null))
                                {
                                    PickReward(item, slotAndReason.Item2);
                                    rewardPicked = true;
                                    break;
                                }
                            }
                        }

                        // Ranged
                        if (_characterSheetManager.RangedSlot.InvTypes.Contains(item.ItemEquipLoc))
                        {
                            string reason = _equipManager.IsRangedBetter(item);
                            if (reason != null)
                            {
                                PickReward(item, reason);
                                rewardPicked = true;
                                break;
                            }
                        }

                        // Trinket
                        if (item.ItemEquipLoc == "INVTYPE_TRINKET")
                        {
                            (ISheetSlot, string) slotAndReason = _equipManager.IsTrinketBetter(item);
                            if (slotAndReason != (null, null))
                            {
                                PickReward(item, slotAndReason.Item2);
                                rewardPicked = true;
                                break;
                            }
                        }

                        // Ring
                        if (item.ItemEquipLoc == "INVTYPE_FINGER")
                        {
                            (ISheetSlot, string) slotAndReason = _equipManager.IsRingBetter(item);
                            if (slotAndReason != (null, null))
                            {
                                PickReward(item, slotAndReason.Item2);
                                rewardPicked = true;
                                break;
                            }
                        }

                        // Armor
                        foreach (ISheetSlot armorSlot in _characterSheetManager.ArmorSlots)
                        {
                            if (armorSlot.InvTypes.Contains(item.ItemEquipLoc))
                            {
                                (ISheetSlot, string) slotAndReason = _equipManager.IsArmorBetter(armorSlot, item);
                                if (slotAndReason != (null, null))
                                {
                                    PickReward(item, slotAndReason.Item2);
                                    rewardPicked = true;
                                    break;
                                }
                            }
                        }

                        if (rewardPicked)
                            break;
                    }
                }

                if (!rewardPicked)
                {
                    itemRewards = itemRewards.OrderByDescending(i => i.ItemSellPrice).ToList();
                    PickReward(itemRewards.First(), "Highest sell value");
                }
            }
        }

        private void PickReward(IWIMItem item, string reason)
        {
            Logger.Log($"Selecting quest reward {item.Name} [{reason}]");
            Lua.LuaDoString($"GetQuestReward({item.RewardSlot})");
        }

        private bool QuestRewardFrameOpen => Lua.LuaDoString<bool>($"return QuestFrameRewardPanel:IsVisible();");
    }
}
