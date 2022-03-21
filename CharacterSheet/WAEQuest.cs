using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

public class WAEQuest
{
    public static bool QuestRewardGossipOpen = false;

    public static void SelectReward(CancelEventArgs cancelable)
    {
        lock (Main.WAELock)
        {
            if (!AutoEquipSettings.CurrentSettings.AutoSelectQuestRewards)
                return;

            int nbQuestRewards = Lua.LuaDoString<int>("return GetNumQuestChoices()");

            if (nbQuestRewards > 0 && QuestRewardGossipOpen)
            {
                cancelable.Cancel = true;

                List<WAEItem> itemRewards = new List<WAEItem>();
                for (int i = 1; i <= nbQuestRewards; i++)
                {
                    string itemLink = Lua.LuaDoString<string>($"return GetQuestItemLink(\"choice\", {i})");
                    itemRewards.Add(new WAEItem(itemLink, rewardSlot: i));
                }

                itemRewards = itemRewards.OrderByDescending(i => i.WeightScore).ToList();
                WAECharacterSheet.Scan();
                WAEContainers.Scan();
                WAEContainers.AllItems.AddRange(itemRewards);

                foreach (WAEItem item in itemRewards)
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
                                WAECharacterSheet.AutoEquipWeapons();
                                continue;
                            }
                        }

                        // Ranged
                        if (WAECharacterSheet.Ranged.InvTypes.Contains(item.ItemEquipLoc))
                        {
                            WAECharacterSheet.AutoEquipRanged();
                            continue;
                        }

                        // Trinket
                        if (item.ItemEquipLoc == "INVTYPE_TRINKET")
                        {
                            WAECharacterSheet.AutoEquipTrinkets();
                            continue;
                        }

                        // Ring
                        if (item.ItemEquipLoc == "INVTYPE_FINGER")
                        {
                            WAECharacterSheet.AutoEquipRings();
                            continue;
                        }

                        // Armor
                        foreach (WAECharacterSheetSlot armorSlot in WAECharacterSheet.ArmorSlots)
                        {
                            if (armorSlot.InvTypes.Contains(item.ItemEquipLoc))
                            {
                                WAECharacterSheet.AutoEquipArmor();
                                break;
                            }
                        }
                    }
                }

                ToolBox.Sleep(3000);
                if (QuestRewardReceived(itemRewards) == null)
                {
                    itemRewards = itemRewards.OrderByDescending(i => i.ItemSellPrice).ToList();
                    Lua.LuaDoString($"GetQuestReward({itemRewards.First().RewardSlot})");
                    ToolBox.Sleep(1000);
                    if (QuestRewardReceived(itemRewards) != null)
                        Logger.Log($"Selected quest reward {QuestRewardReceived(itemRewards).Name} because it has the highest sell value");
                }

                QuestRewardGossipOpen = false;
            }
        }
    }

    private static WAEItem QuestRewardReceived(List<WAEItem> itemRewards)
    {
        WAEContainers.Scan();
        foreach (WAEItem reward in itemRewards)
        {
            foreach (WAEItem bagItem in WAEContainers.AllItems)
            {
                if (bagItem.ItemLink == reward.ItemLink)
                    return reward;
            }
        }
        return null;
    }
}
