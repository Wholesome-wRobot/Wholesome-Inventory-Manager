using System.Collections.Generic;
using System.Threading;
using System.Linq;
using wManager.Wow.Helpers;
using System.Globalization;
using wManager.Wow.ObjectManager;
using static WAEEnums;
using wManager;
using Wholesome_Inventory_Manager.CharacterSheet;
using System.Text.RegularExpressions;

public class WAEItem
{
    private readonly int _maxNbEquipAttempts = 5;
    public static List<string> ItemEquipAttempts { get; set; } = new List<string>();
    public uint ItemId { get; set; }
    public string Name { get; set; }
    public string ItemLink { get; set; }
    public int ItemRarity { get; set; }
    public int ItemLevel { get; set; }
    public int ItemMinLevel { get; set; }
    public string ItemType { get; set; }
    public string ItemSubType { get; set; }
    public int ItemStackCount { get; set; }
    public string ItemEquipLoc { get; set; }
    public string ItemTexture { get; set; }
    public int ItemSellPrice { get; set; }
    public int BagCapacity { get; set; }
    public int QuiverCapacity { get; set; }
    public int AmmoPouchCapacity { get; set; }
    public int InBag { get; set; } = -1;
    public int InBagSlot { get; set; } = -1;
    public double UniqueId { get; set; }
    public float WeightScore { get; set; } = 0;
    public Dictionary<string, float> ItemStats { get; set; } = new Dictionary<string, float>(){};
    public float WeaponSpeed { get; set; } = 0;
    public int RewardSlot { get; set; } = -1;
    public int RollId { get; set; } = -1;
    public bool HasBeenRolled { get; set; } = false;

    private static int UniqueIdCounter = 0;

    public WAEItem(string itemLink, 
        int rewardSlot = -1, 
        int rollId = -1, 
        int inBag = -1, 
        int inBagSlot = -1)
    {
        UniqueId = ++UniqueIdCounter;
        ItemLink = itemLink;
        RewardSlot = rewardSlot;
        RollId = rollId;
        InBag = inBag;
        InBagSlot = inBagSlot;

        if (itemLink.Length < 10)
            return;

        RecordItemId(itemLink);

        WAEItem existingCopy = WAEItemDB.Get(ItemLink);

        if (existingCopy != null)
            CloneFromDB(existingCopy);
        else
        {
            string iteminfo = Lua.LuaDoString<string>($@"
                itemName, itemLink, itemRarity, itemLevel, itemMinLevel, itemType,
                itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(""{ItemLink.Replace("\"", "\\\"")}"");

                if (itemSellPrice == null) then
                    itemSellPrice = 0
                end

                if (itemEquipLoc == null) then
                    itemEquipLoc = ''
                end

                return itemName..'§'..itemLink..'§'..itemRarity..'§'..itemLevel..
                '§'..itemMinLevel..'§'..itemType..'§'..itemSubType..'§'..itemStackCount..
                '§'..itemEquipLoc..'§'..itemTexture..'§'..itemSellPrice");

            string[] infoArray = iteminfo.Split('§');

            if (infoArray.Length < 11)
            {
                Logger.LogDebug($"Item {itemLink} doesn't have the correct number of info. Skipping.");
                return;
            }

            Name = infoArray[0];
            ItemLink = itemLink;
            ItemRarity = int.Parse(infoArray[2]);
            ItemLevel = int.Parse(infoArray[3]);
            ItemMinLevel = int.Parse(infoArray[4]);
            ItemType = infoArray[5];
            ItemSubType = infoArray[6];
            ItemStackCount = int.Parse(infoArray[7]);
            ItemEquipLoc = infoArray[8];
            ItemTexture = infoArray[9];
            ItemSellPrice = int.Parse(infoArray[10]);

            if (Main.WoWVersion <= ToolBox.WoWVersion.TBC)
            {
                RecordToolTipTBC();
                RecordBagSpaceTBC();
            }
            else
            {
                RecordToolTipWotLK();
                RecordStatsWotLK();
            }


            WAEItemDB.Add(this);
            if (AutoEquipSettings.CurrentSettings.LogItemInfo)
                LogItemInfo();
        }
    }

    private void RecordItemId(string itemLink)
    {
        if (itemLink.Split(':').Length < 2)
        {
            Logger.LogError($"[{Name}] Couldn't parse item ID from item link {itemLink}");
            return;
        }

        uint parsedItemId = 0;
        if (uint.TryParse(itemLink.Split(':')[1], out parsedItemId))
            ItemId = parsedItemId;
        else
            Logger.LogError($"Couldn't parse item ID {itemLink.Split(':')[1]}");
    }

    private void CloneFromDB(WAEItem existingCopy)
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

    public void RecordBagSpaceTBC()
    {
        if (ItemSubType == "Bag" || ItemSubType == "Quiver" || ItemSubType == "Ammo Pouch")
        {
            if (TBCBags.ContainsKey(ItemId))
                BagCapacity = TBCBags[ItemId];

            if (TBCQuivers.ContainsKey(ItemId))
                QuiverCapacity = TBCQuivers[ItemId];

            if (TBCAmmoPouches.ContainsKey(ItemId))
                AmmoPouchCapacity = TBCAmmoPouches[ItemId];

            if (BagCapacity == 0 && AmmoPouchCapacity == 0 && QuiverCapacity == 0)
                Logger.LogError($"We don't know the capacity of {Name}");
        }
    }

    public void RecordToolTipTBC()
    {

        if (ItemType != "Armor" && ItemType != "Weapon")
            return;

        // Record the info present in the tooltip
        string lines = Lua.LuaDoString<string>($@"
            WEquipTooltip:ClearLines()
            WEquipTooltip:SetHyperlink(""{ItemLink}"")
            return EnumerateTooltipLines(WEquipTooltip: GetRegions())");

        string[] allLines = lines.Split('|');
        foreach (string l in allLines)
        {
            bool lineRecorded = false;
            if (l.Length > 0 
                && l != "r" 
                && !l.Contains("Socket") 
                && !l.Contains("Requires") 
                && !l.Contains(Name) 
                && !l.Contains("Binds")
                && !l.Contains("Unique"))
            {
                // Loof for item stats
                foreach (KeyValuePair<string, CharStat> statEnum in StatEnums)
                {
                    if (l.ToLower().Contains(statEnum.Key.ToLower()))
                    {
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
                            Logger.LogError($"No value found for {statEnum.Value}");

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
        RecordWeightScore();
    }

    public void RecordToolTipWotLK()
    {
        // Record the info present in the tooltip
        string lines = Lua.LuaDoString<string>($@"
            WEquipTooltip:ClearLines()
            WEquipTooltip:SetHyperlink(""{ItemLink}"")
            return EnumerateTooltipLines(WEquipTooltip: GetRegions())");

        string[] allLines = lines.Split('|');
        foreach (string l in allLines)
        {
            if (l.Length > 0)
            {
                // record specifics
                if (ItemType == "Weapon" && l.Contains("Speed "))
                    WeaponSpeed = float.Parse(l.Replace("Speed ", "").Replace(".", ","));
                if (l.Contains(" Slot Bag"))
                    BagCapacity = int.Parse(l.Replace(" Slot Bag", ""));
                else if (l.Contains(" Slot Quiver"))
                    QuiverCapacity = int.Parse(l.Replace(" Slot Quiver", ""));
                else if (l.Contains(" Slot Ammo Pouch"))
                    AmmoPouchCapacity = int.Parse(l.Replace(" Slot Ammo Pouch", ""));
            }
        }
    }

    public void RecordStatsWotLK()
    {
        if (ItemType != "Armor" && ItemType != "Weapon")
            return;

        string stats = Lua.LuaDoString<string>($@"
                local itemstats=GetItemStats(""{ItemLink.Replace("\"", "\\\"")}"")
                local stats = """"
                for stat, value in pairs(itemstats) do
                    stats = stats.._G[stat]..""§""..value..""$""
                end
                return stats");
        //Logger.Log("stats -> " + stats);
        if (stats.Length < 1)
            return;

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
        RecordWeightScore();
    }

    private void RecordWeightScore()
    {
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

    public float GetOffHandWeightScore()
    {
        if (ItemStats.ContainsKey("Damage Per Second"))
            return WeightScore - (ItemStats["Damage Per Second"] * AutoEquipSettings.CurrentSettings.GetStat(CharStat.DamagePerSecond)) / 2;
        return WeightScore;
    }

    public void DeleteFromBag(string reason)
    {
        if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(Name))
            return;

        Logger.Log($"Deleting {Name} ({reason})");
        Lua.LuaDoString($"PickupContainerItem({InBag}, {InBagSlot});");
        Lua.LuaDoString("DeleteCursorItem();");
        ToolBox.Sleep(100);
    }

    public void Use()
    {
        if (InBag < 0 || InBagSlot < 0)
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        else
            Lua.LuaDoString($"UseContainerItem({InBag}, {InBagSlot})");
    }

    public bool EquipSelectRoll(int slotId, string reason)
    {
        WAELootFilter.ProtectFromFilter(ItemLink);

        // ROLL
        if (RollId >= 0)
        {
            WAEGroupRoll.Roll(RollId, this, reason, RollType.NEED);
            HasBeenRolled = true;
            WAEContainers.AllItems.Clear();
            return true;
        }

        // SELECT REWARD
        if (RewardSlot >= 0)
        {
            Lua.LuaDoString($"GetQuestReward({RewardSlot})");
            Logger.Log($"Selecting quest reward {Name} [{reason}]");
            WAEContainers.AllItems.Clear();
            return true;
        }

        // EQUIP
        WAECharacterSheetSlot slot = WAECharacterSheet.AllSlots.Find(s => s.InventorySlotID == slotId);
        if (slot.Item?.ItemLink == ItemLink)
            return true;

        if (ItemSubType != "Arrow" && ItemSubType != "Bullet")
            if (ObjectManager.Me.InCombatFlagOnly || ObjectManager.Me.IsCast)
                return false;

        if (InBag < 0 || InBagSlot < 0)
        {
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        }
        else
        {
            Logger.Log($"Equipping {Name} ({WeightScore}) [{reason}]");
            ItemEquipAttempts.Add(ItemLink);
            PickupFromBag();
            DropInInventory(slotId);
            ToolBox.Sleep(100);
            Lua.LuaDoString($"EquipPendingItem(0);");
            Lua.LuaDoString($"StaticPopup1Button1:Click()");
            ToolBox.Sleep(200);
            WAECharacterSheet.Scan();
            WAEContainers.Scan();
            WAECharacterSheetSlot updatedSlot = WAECharacterSheet.AllSlots.Find(s => s.InventorySlotID == slotId);
            if (updatedSlot.Item == null || updatedSlot.Item.ItemLink != ItemLink)
            {
                if (GetNbEquipAttempts() < _maxNbEquipAttempts)
                    Logger.LogError($"Failed to equip {Name}. Retrying soon ({GetNbEquipAttempts()}).");
                else
                    Logger.LogError($"Failed to equip {Name} after {GetNbEquipAttempts()} attempts.");

                Lua.LuaDoString($"ClearCursor()");
                return false;
            }
            ItemEquipAttempts.RemoveAll(i => i == ItemLink);
            WAELootFilter.AllowForFilter(ItemLink);
            return true;

        }
        return false;
    }

    public int GetNbEquipAttempts()
    {
        return ItemEquipAttempts.FindAll(i => i == ItemLink).Count;
    }

    public void DropInInventory(int slotId)
    {
        Lua.LuaDoString($"PickupInventoryItem({slotId});");
    }

    public void PickupFromBag()
    {
        Lua.LuaDoString($"ClearCursor(); PickupContainerItem({InBag}, {InBagSlot});");
    }

    public bool CanEquip()
    {
        if (ItemSubType == "" || !ItemSkillsDictionary.ContainsKey(ItemSubType)
            && ItemSubType != "Miscellaneous")
            return false;

        bool skillCheckOK = ItemSubType == "Miscellaneous" 
            || WAECharacterSheet.MySkills.ContainsKey(ItemSubType) && WAECharacterSheet.MySkills[ItemSubType] > 0
            || ItemSubType == "Fist Weapons" && Skill.Has(wManager.Wow.Enums.SkillLine.FistWeapons);
        
        return ObjectManager.Me.Level >= ItemMinLevel && skillCheckOK && GetNbEquipAttempts() < _maxNbEquipAttempts;
    }

    public bool MoveToBag(int position, int slot)
    {
        Lua.LuaDoString($"PickupContainerItem({position}, {slot});"); // en fait un clique sur le slot de destination
        ToolBox.Sleep(200);
        if (WAEContainers.ListContainers.Find(bag => bag.Position == position).GetContainerItemlink(slot) == ItemLink)
            return true;
        Logger.LogError($"Couldn't move {Name} to bag {position} slot {slot}, retrying soon.");
        return false;
    }

    public void MoveToBag(int position)
    {
        PickupFromBag();
        ToolBox.Sleep(100);
        int bagSlot = 19 + position;
        Lua.LuaDoString($"PutItemInBag({bagSlot})");
        Thread.Sleep(100);
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
                    | InBag: {InBag} | InBagSlot: {InBagSlot} | ItemId: {ItemId} | WEIGHT SCORE : {WeightScore}
                    {stats}");
    }
}