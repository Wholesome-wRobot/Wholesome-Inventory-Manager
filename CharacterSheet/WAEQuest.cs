using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using wManager.Wow.Helpers;

public class WAEQuest
{
    public static bool SelectingReward = false;
    public static bool QuestRewardGossipOpen = false;

    public static void SelectReward(CancelEventArgs cancelable)
    {
        if (!AutoEquipSettings.CurrentSettings.AutoSelectQuestRewards)
            return;

        cancelable.Cancel = true;
        SelectingReward = true;

        int nbQuestRewards = Lua.LuaDoString<int>("return GetNumQuestChoices()");

        if (nbQuestRewards > 0 && QuestRewardGossipOpen)
        {
            //Logger.Log("SELECTING REWARD");
            List<WAEItem> itemRewards = new List<WAEItem>();
            for (int i = 1; i <= nbQuestRewards; i++)
            {
                string itemLink = Lua.LuaDoString<string>($"return GetQuestItemLink(\"choice\", {i})");
                itemRewards.Add(new WAEItem(itemLink, i));
            }

            itemRewards = itemRewards.OrderByDescending(i => i.WeightScore).ToList();
            WAECharacterSheet.Scan();
            WAEContainers.Scan();
            WAEContainers.AllItems.AddRange(itemRewards);

            foreach (WAEItem item in itemRewards)
            {
                // Weapons
                if (WAEEnums.TwoHanders.Contains(WAEEnums.ItemSkillsDictionary[item.ItemSubType])
                    || WAEEnums.OneHanders.Contains(WAEEnums.ItemSkillsDictionary[item.ItemSubType])
                    || item.ItemSubType == "Miscellaneous")
                    WAECharacterSheet.AutoEquipWeapons();

                // Ranged
                if (WAECharacterSheet.Ranged.InvTypes.Contains(item.ItemEquipLoc))
                    WAECharacterSheet.AutoEquipRanged();

                // Trinket
                if (item.ItemEquipLoc == "INVTYPE_TRINKET")
                    WAECharacterSheet.AutoEquipTrinkets();

                // Ring
                if (item.ItemEquipLoc == "INVTYPE_FINGER")
                    WAECharacterSheet.AutoEquipRings();

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

            ToolBox.Sleep(3000);
            if (QuestRewardReceived(itemRewards) == null)
            {
                itemRewards = itemRewards.OrderByDescending(i => i.ItemSellPrice).ToList();
                Lua.LuaDoString($"GetQuestReward({itemRewards.First().RewardSlot})");
                ToolBox.Sleep(1000);
                if (QuestRewardReceived(itemRewards) != null)
                    Logger.Log($"Selected quest reward {QuestRewardReceived(itemRewards).Name} because it has the highest sell value");
            }
        }

        SelectingReward = false;
        QuestRewardGossipOpen = false;
    }

    private static WAEItem QuestRewardReceived(List<WAEItem> itemRewards)
    {
        WAEContainers.Scan();
        foreach (WAEItem reward in itemRewards)
        {
            foreach (WAEItem bagItem in WAEContainers.AllItems)
            {
                if (bagItem.ItemLink == reward.ItemLink)
                {
                    //Logger.Log($"{reward.Name} detected in inventory");
                    return reward;
                }
            }
        }
        //Logger.Log($"No reward detected in inventory");
        return null;
    }
}
