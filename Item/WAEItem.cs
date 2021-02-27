using System.Collections.Generic;
using System.Threading;
using System.Linq;
using wManager.Wow.Helpers;
using System.Globalization;
using wManager.Wow.ObjectManager;
using static WAEEnums;
using wManager;

public class WAEItem
{
    public static List<string> ItemEquipAttempts { get; set; } = new List<string>();
    public int ItemId { get; set; }
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

    private static int UniqueIdCounter = 0;

    public WAEItem(string itemLink)
    {
        ItemLink = itemLink;
        UniqueId = ++UniqueIdCounter;

        WAEItem existingCopy = WAEItemDB.Get(ItemLink);

        if (existingCopy != null)
            CloneFromDB(existingCopy);
        else
        {
            string iteminfo = Lua.LuaDoString<string>($@"
            itemName, itemLink, itemRarity, itemLevel, itemMinLevel, itemType,
            itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(""{ItemLink.Replace("\"", "\\\"")}"");

            return itemName..'§'..itemLink..'§'..itemRarity..'§'..itemLevel..
            '§'..itemMinLevel..'§'..itemType..'§'..itemSubType..'§'..itemStackCount..
            '§'..itemEquipLoc..'§'..itemTexture..'§'..itemSellPrice");
            
            string[] infoArray = iteminfo.Split('§');
            Name = infoArray[0];
            ItemLink = infoArray[1];
            ItemRarity = int.Parse(infoArray[2]);
            ItemLevel = int.Parse(infoArray[3]);
            ItemMinLevel = int.Parse(infoArray[4]);
            ItemType = infoArray[5];
            ItemSubType = infoArray[6];
            ItemStackCount = int.Parse(infoArray[7]);
            ItemEquipLoc = infoArray[8];
            ItemTexture = infoArray[9];
            ItemSellPrice = int.Parse(infoArray[10]);

            RecordToolTip();
            RecordStats();
            //LogItemInfo();
            WAEItemDB.Add(this);
        }
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
    }

    public void RecordStats()
    {
        if (ItemType != "Armor" && ItemType != "Weapon")
            return;

        string stats = Lua.LuaDoString<string>($@"local itemstats=GetItemStats(""{ItemLink.Replace("\"", "\\\"")}"") 
                local stats = """" 
                for stat, value in pairs(itemstats) do 
                    stats = stats.._G[stat]..""§""..value..""$"" 
                end
                return stats");

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
        //Logger.LogDebug(Name);
        foreach (KeyValuePair<string, float> entry in ItemStats)
        {
            if (StatEnums.ContainsKey(entry.Key))
            {
                CharStat statEnum = StatEnums[entry.Key];
                WeightScore += entry.Value * AutoEquipSettings.CurrentSettings.GetStat(statEnum);
                //Logger.LogDebug(entry.Key + " -> " + (entry.Value * AutoEquipSettings.CurrentSettings.GetStat(statEnum)).ToString());
            }
            else
            {
                if (!entry.Key.Contains("Socket"))
                    Logger.LogError("Can't detect : " + entry.Key);
            }
        }
        WeightScore += ItemLevel;
        //Logger.LogDebug("Item Level -> " + ItemLevel);
        //Logger.LogDebug("Total : " + WeightScore.ToString()); ;
    }

    public float GetOffHandWeightScore()
    {
        if (ItemStats.ContainsKey("Damage Per Second"))
            return WeightScore - (ItemStats["Damage Per Second"] * AutoEquipSettings.CurrentSettings.GetStat(CharStat.DamagePerSecond)) / 2;
        return WeightScore;
    }

    public void RecordToolTip()
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

    public void DeleteFromBag(string reason)
    {
        if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(Name))
            return;

        Logger.Log($"Deleting {Name} ({reason})");
        Lua.LuaDoString($"PickupContainerItem({InBag}, {InBagSlot});");
        Lua.LuaDoString("DeleteCursorItem();");
        Thread.Sleep(100);
    }

    public void Use()
    {
        if (InBag < 0 || InBagSlot < 0)
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        else
            Lua.LuaDoString($"UseContainerItem({InBag}, {InBagSlot})");
    }

    public bool Equip(int slotId, bool log = false)
    {
        WAECharacterSheetSlot slot = WAECharacterSheet.AllSlots.Find(s => s.InventorySlotID == slotId);
        if (slot.Item?.ItemLink == ItemLink)
            return true;

        if (ObjectManager.Me.InCombatFlagOnly)
            return false;

        if (InBag < 0 || InBagSlot < 0)
        {
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        }
        else
        {
            if (log)
                Logger.Log($"Equipping {Name} ({WeightScore})");
            ItemEquipAttempts.Add(ItemLink);
            PickupFromBag();
            DropInInventory(slotId);
            Thread.Sleep(100);
            Lua.LuaDoString($"EquipPendingItem(0);");
            Lua.LuaDoString($"StaticPopup1Button1:Click()");
            Thread.Sleep(200);
            WAECharacterSheet.Scan();
            WAEContainers.Scan();
            WAECharacterSheetSlot updatedSlot = WAECharacterSheet.AllSlots.Find(s => s.InventorySlotID == slotId);
            if (updatedSlot.Item == null || updatedSlot.Item.ItemLink != ItemLink)
            {
                Logger.LogError($"Failed to equip {Name}. Retrying soon ({GetNbEquipAttempts()}).");
                Lua.LuaDoString($"ClearCursor()");
                return false;
            }
            ItemEquipAttempts.RemoveAll(i => i == ItemLink);
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
        if (!ItemSkillsDictionary.ContainsKey(ItemSubType)
            && ItemSubType != "Miscellaneous")
            return false;

        bool skillCheckOK = ItemSubType == "Miscellaneous" 
            || WAECharacterSheet.MySkills.ContainsKey(ItemSubType) && WAECharacterSheet.MySkills[ItemSubType] > 0
            || ItemSubType == "Fist Weapons" && Skill.Has(wManager.Wow.Enums.SkillLine.FistWeapons);
        //Logger.Log($"{Name} - {ItemSubType} - {ItemEquipLoc}");
        return ObjectManager.Me.Level >= ItemMinLevel && skillCheckOK && GetNbEquipAttempts() < 5;
    }

    public bool MoveToBag(int position, int slot)
    {
        Lua.LuaDoString($"PickupContainerItem({position}, {slot});"); // en fait un clique sur le slot de destination
        Thread.Sleep(200);
        if (WAEContainers.ListContainers.Find(bag => bag.Position == position).GetContainerItemlink(slot) == ItemLink)
            return true;
        Logger.LogError($"Couldn't move {Name} to bag {position} slot {slot}, retrying soon.");
        return false;
    }

    public void MoveToBag(int position)
    {
        PickupFromBag();
        Thread.Sleep(100);
        int bagSlot = 19 + position;
        Lua.LuaDoString($"PutItemInBag({bagSlot})");
        Thread.Sleep(100);
    }

    public void LogItemInfo()
    {
        Logger.LogDebug($@"Name : {Name} | ItemLink : {ItemLink} | ItemRarity : {ItemRarity} | ItemLevel : {ItemLevel} | ItemMinLevel : {ItemMinLevel}
                    | ItemType : {ItemType} | ItemSubType : {ItemSubType} | ItemStackCount : {ItemStackCount} |ItemEquipLoc : {ItemEquipLoc}
                    | ItemSellPrice : {ItemSellPrice} | QuiverCapacity : {QuiverCapacity} | AmmoPouchCapacity : {AmmoPouchCapacity}
                    | BagCapacity : {BagCapacity} | UniqueId : {UniqueId} | WEIGHT SCORE : {WeightScore}");
    }
}