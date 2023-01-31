using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Managers.Items
{
    internal class WIMItem : IWIMItem
    {
        private int _uniqueIdCounter = 0;

        public uint ItemId { get; private set; }
        public string Name { get; private set; }
        public string ItemLink { get; private set; }
        public int ItemRarity { get; private set; }
        public int ItemLevel { get; private set; }
        public int ItemMinLevel { get; private set; }
        public string ItemType { get; private set; }
        public string ItemSubType { get; private set; }
        public int ItemStackCount { get; private set; }
        public string ItemEquipLoc { get; private set; }
        public string ItemTexture { get; private set; }
        public int ItemSellPrice { get; private set; }
        public int BagCapacity { get; private set; }
        public int QuiverCapacity { get; private set; }
        public int AmmoPouchCapacity { get; private set; }
        public int BagIndex { get; private set; } = -1;
        public int SlotIndex { get; private set; } = -1;
        public double UniqueId { get; private set; }
        public float WeightScore { get; private set; } = 0;
        public float WeaponSpeed { get; private set; } = 0;
        public int RewardSlot { get; private set; } = -1;
        public int RollId { get; private set; } = -1;
        public bool HasBeenRolled { get; set; }
        public bool IsUnique { get; set; }
        public Dictionary<string, float> ItemStats { get; private set; } = new Dictionary<string, float>() { };

        public WIMItem(
            string itemLink,
            int rewardSlot = -1,
            int rollId = -1,
            int inBag = -1,
            int inBagSlot = -1)
        {
            UniqueId = ++_uniqueIdCounter;
            ItemLink = itemLink;
            RewardSlot = rewardSlot;
            RollId = rollId;
            BagIndex = inBag;
            SlotIndex = inBagSlot;

            if (ItemLink.Length < 10)
                return;

            IWIMItem existingCopy = ItemCache.Get(ItemLink);
            if (existingCopy != null)
            {
                CloneFromDB(existingCopy);
            }
            else
            {
                string iteminfo = Lua.LuaDoString<string>($@"return ParseItemInfo(-1, -1, ""{ItemLink.Replace("\"", "\\\"")}"");");
                string[] infoArray = iteminfo.Split('£');
                RecordAllItemInfo(infoArray);
            }
        }

        public WIMItem(
            string[] itemInfo,
            int bagIndex = -1,
            int slotIndex = -1)
        {
            ItemLink = itemInfo[2];
            UniqueId = ++_uniqueIdCounter;
            BagIndex = bagIndex;
            SlotIndex = slotIndex;

            if (ItemLink.Length < 10)
                return;

            IWIMItem existingCopy = ItemCache.Get(ItemLink);
            if (existingCopy != null)
            {
                CloneFromDB(existingCopy);
            }
            else
            {
                RecordAllItemInfo(itemInfo);
            }

        }

        private void RecordAllItemInfo(string[] luaItemInfo)
        {
            if (luaItemInfo.Length < 11)
            {
                Logger.LogDebug($"Item {luaItemInfo[3]} doesn't have the correct number of info. Skipping.");
                return;
            }

            if (uint.TryParse(ItemLink.Split(':')[1], out uint parsedItemId))
            {
                ItemId = parsedItemId;
            }
            else
            {
                Logger.LogError($"Couldn't parse item ID {ItemLink.Split(':')[1]}");
            }

            Name = luaItemInfo[3];
            ItemRarity = ToolBox.ParseInt(luaItemInfo[4]);
            ItemLevel = ToolBox.ParseInt(luaItemInfo[5]);
            ItemMinLevel = ToolBox.ParseInt(luaItemInfo[6]);
            ItemType = luaItemInfo[7];
            ItemSubType = luaItemInfo[8];
            ItemStackCount = ToolBox.ParseInt(luaItemInfo[9]);
            ItemEquipLoc = luaItemInfo[10];
            ItemSellPrice = ToolBox.ParseInt(luaItemInfo[11]);

            string toolTip = luaItemInfo[12];

            if (Main.WoWVersion <= ToolBox.WoWVersion.TBC)
            {
                RecordToolTipTBC(toolTip);
                RecordBagSpaceTBC();
            }
            else
            {
                RecordToolTipWotLK(toolTip);
                RecordStatsWotLK();
            }

            RecordWeightScore();

            ItemCache.Add(this);
            if (AutoEquipSettings.CurrentSettings.LogItemInfo)
                LogItemInfo();
        }

        private void CloneFromDB(IWIMItem existingCopy)
        {
            Name = existingCopy.Name;
            ItemLink = existingCopy.ItemLink;
            ItemRarity = existingCopy.ItemRarity;
            ItemLevel = existingCopy.ItemLevel;
            ItemMinLevel = existingCopy.ItemMinLevel;
            ItemType = existingCopy.ItemType;
            ItemSubType = existingCopy.ItemSubType;
            ItemStackCount = existingCopy.ItemStackCount;
            ItemEquipLoc = existingCopy.ItemEquipLoc;
            ItemTexture = existingCopy.ItemTexture;
            ItemSellPrice = existingCopy.ItemSellPrice;
            BagCapacity = existingCopy.BagCapacity;
            QuiverCapacity = existingCopy.QuiverCapacity;
            AmmoPouchCapacity = existingCopy.AmmoPouchCapacity;
            UniqueId = existingCopy.UniqueId;
            WeightScore = existingCopy.WeightScore;
            ItemStats = existingCopy.ItemStats;
            WeaponSpeed = existingCopy.WeaponSpeed;
            ItemId = existingCopy.ItemId;
        }

        private void RecordBagSpaceTBC()
        {
            if (ItemSubType == "Bag" || ItemSubType == "Quiver" || ItemSubType == "Ammo Pouch")
            {
                if (TBCBags.ContainsKey(ItemId))
                {
                    BagCapacity = TBCBags[ItemId];
                }

                if (TBCQuivers.ContainsKey(ItemId))
                {
                    QuiverCapacity = TBCQuivers[ItemId];
                }

                if (TBCAmmoPouches.ContainsKey(ItemId))
                {
                    AmmoPouchCapacity = TBCAmmoPouches[ItemId];
                }

                if (BagCapacity == 0 && AmmoPouchCapacity == 0 && QuiverCapacity == 0)
                {
                    Logger.LogError($"We don't know the capacity of {Name}");
                }
            }
        }

        private void RecordToolTipTBC(string lines)
        {
            if (ItemType != "Armor" && ItemType != "Weapon")
            {
                return;
            }

            if (lines == null)
            {
                Logger.LogError($"Failed to record tooltip for " + Name);
                return;
            }

            string[] allLines = lines.Split('|');
            foreach (string l in allLines)
            {
                bool lineRecorded = false;
                if (l.Length > 0
                    && l != "r"
                    && !l.Contains("Socket")
                    && !l.Contains("Requires")
                    && !l.Contains(Name)
                    && !l.Contains("Binds"))
                {
                    if (l.Contains("Unique"))
                    {
                        IsUnique = true;
                        continue;
                    }

                    // Look for item stats
                    foreach (KeyValuePair<string, CharStat> statEnum in StatEnums)
                    {
                        if (l.ToLower().Contains(statEnum.Key.ToLower()))
                        {
                            if (ObjectManager.Me.WowClass != WoWClass.Druid && l.Contains("Cat, Bear"))
                            {
                                continue;
                            }

                            string line = l.Replace(".", ",").Replace("(", "").Replace(")", "");

                            if (line.Contains("and damage done"))
                            {
                                Match prefix = Regex.Match(line, @"^.*?(?=and damage done)");
                                line = line.Replace(prefix.Value, "");
                            }

                            string[] words = line.Split(' ');

                            string value = string.Empty;
                            bool statNumberFound = false;

                            foreach (string word in words)
                            {
                                for (int i = 0; i < word.Length; i++)
                                {
                                    if (char.IsDigit(word[i]) || word[i] == ',')
                                    {
                                        value += word[i];
                                        statNumberFound = true;
                                    }
                                }
                                if (statNumberFound)
                                    break;
                            }

                            if (value.Length > 0 && !ItemStats.ContainsKey(statEnum.Key))
                            {
                                ItemStats.Add(statEnum.Key, float.Parse(value));
                                lineRecorded = true;
                            }
                            else
                            {
                                Logger.LogError($"No value found for {statEnum.Value}");
                            }
                            break;
                        }
                    }

                    if (lineRecorded)
                        continue;

                    // record specifics
                    if (ItemType == "Weapon" && l.Contains("Speed "))
                        WeaponSpeed = float.Parse(l.Replace("Speed ", "").Replace(".", ","));

                    /*if (!lineRecorded)
                        Logger.LogError($"Ignored : {l}");*/
                }
            }
        }

        private void RecordToolTipWotLK(string lines)
        {
            if (lines == null)
            {
                Logger.LogError($"Failed to record tooltip for " + Name);
                return;
            }

            string[] allLines = lines.Split('|');
            foreach (string l in allLines)
            {
                if (l.Length > 0)
                {
                    // record specifics
                    if (ItemType == "Weapon" && l.Contains("Speed "))
                    {
                        WeaponSpeed = float.Parse(l.Replace("Speed ", "").Replace(".", ","));
                    }
                    if (l.Contains(" Slot Bag"))
                    {
                        BagCapacity = int.Parse(l.Replace(" Slot Bag", ""));
                    }
                    else if (l.Contains(" Slot Quiver"))
                    {
                        QuiverCapacity = int.Parse(l.Replace(" Slot Quiver", ""));
                    }
                    else if (l.Contains(" Slot Ammo Pouch"))
                    {
                        AmmoPouchCapacity = int.Parse(l.Replace(" Slot Ammo Pouch", ""));
                    }
                    else if (l.Contains("Unique"))
                    {
                        IsUnique = true;
                    }
                }
            }
        }

        private void RecordStatsWotLK()
        {
            if (ItemType != "Armor" && ItemType != "Weapon")
            {
                return;
            }

            string stats = Lua.LuaDoString<string>($@"
                    local itemstats=GetItemStats(""{ItemLink.Replace("\"", "\\\"")}"")
                    local stats = """"
                    for stat, value in pairs(itemstats) do
                        stats = stats.._G[stat]..""§""..value..""$""
                    end
                    return stats
                ");

            if (stats.Length < 1)
            {
                return;
            }

            List<string> statsPairs = stats.Split('$').ToList();
            foreach (string pair in statsPairs)
            {
                if (pair.Length > 0)
                {
                    string[] statsPair = pair.Split('§');
                    string statName = statsPair[0];
                    float statValue = float.Parse(statsPair[1], CultureInfo.InvariantCulture);
                    if (!ItemStats.ContainsKey(statName))
                        ItemStats.Add(statName, statValue);
                }
            }
        }

        private void RecordWeightScore()
        {
            AdjustDPSScore();

            foreach (KeyValuePair<string, float> entry in ItemStats)
            {
                if (StatEnums.ContainsKey(entry.Key))
                {
                    CharStat statEnum = StatEnums[entry.Key];
                    WeightScore += entry.Value * AutoEquipSettings.CurrentSettings.GetStat(statEnum);
                }
            }

            WeightScore += ItemLevel;
        }

        private void AdjustDPSScore()
        {
            if (ItemStats.ContainsKey("Damage Per Second"))
            {
                WoWClass myClass = ObjectManager.Me.WowClass;

                // Ranged weapons
                if (ItemEquipLoc == "INVTYPE_RANGEDRIGHT"
                    || ItemEquipLoc == "INVTYPE_RANGED"
                    || ItemEquipLoc == "INVTYPE_THROWN")
                {
                    if (myClass == WoWClass.Druid
                        || myClass == WoWClass.Rogue
                        || myClass == WoWClass.Warrior)
                    {
                        Logger.LogDebug($"Adjusting {Name} DPS ({ItemStats["Damage Per Second"]}) to {ItemStats["Damage Per Second"] / 20}");
                        ItemStats["Damage Per Second"] = ItemStats["Damage Per Second"] / 20;
                    }
                }
                // Melee weapons
                else
                {
                    if (myClass == WoWClass.Hunter)
                    {
                        Logger.LogDebug($"Adjusting {Name} DPS ({ItemStats["Damage Per Second"]}) to {ItemStats["Damage Per Second"] / 20}");
                        ItemStats["Damage Per Second"] = ItemStats["Damage Per Second"] / 20;
                    }
                }
            }
        }

        public float GetOffHandWeightScore()
        {
            if (ItemStats.ContainsKey("Damage Per Second"))
            {
                return WeightScore - (ItemStats["Damage Per Second"] * AutoEquipSettings.CurrentSettings.GetStat(CharStat.DamagePerSecond)) / 2;
            }

            return WeightScore;
        }

        public void DeleteFromBag(string reason)
        {
            if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(Name))
            {
                return;
            }

            Logger.Log($"Deleting {Name} ({reason})");
            Lua.LuaDoString($"PickupContainerItem({BagIndex}, {SlotIndex});");
            Lua.LuaDoString("DeleteCursorItem();");
            ToolBox.Sleep(200);
        }

        private void Use()
        {
            if (BagIndex < 0 || SlotIndex < 0)
            {
                Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
            }
            else
            {
                Lua.LuaDoString($"UseContainerItem({BagIndex}, {SlotIndex})");
            }
        }

        public void LogItemInfo()
        {
            string stats = "";
            if (ItemStats.Count > 0)
            {
                stats = "STATS: ";
                foreach (KeyValuePair<string, float> stat in ItemStats)
                {
                    stats += $"{stat.Key}:{stat.Value} ";
                }
            }

            Logger.LogDebug($@"Name : {Name} | ItemLink : {ItemLink} | ItemRarity : {ItemRarity} | ItemLevel : {ItemLevel} | ItemMinLevel : {ItemMinLevel}
                    | ItemType : {ItemType} | ItemSubType : {ItemSubType} | ItemStackCount : {ItemStackCount} |ItemEquipLoc : {ItemEquipLoc}
                    | ItemSellPrice : {ItemSellPrice} | QuiverCapacity : {QuiverCapacity} | AmmoPouchCapacity : {AmmoPouchCapacity}
                    | BagCapacity : {BagCapacity} | WeaponSpeed : {WeaponSpeed} | UniqueId : {UniqueId} | Reward Slot: {RewardSlot} | RollID: {RollId} 
                    | InBag: {BagIndex} | InBagSlot: {SlotIndex} | ItemId: {ItemId} | WEIGHT SCORE : {WeightScore}
                    {stats}");
        }

        //private int GetNbEquipAttempts => _itemEquipAttempts.FindAll(i => i == ItemLink).Count;
        public void ClickInInventory(int slotId) => Lua.LuaDoString($"PickupInventoryItem({slotId});");
        public void PickupFromBag() => Lua.LuaDoString($"ClearCursor(); PickupContainerItem({BagIndex}, {SlotIndex});");
    }
}
