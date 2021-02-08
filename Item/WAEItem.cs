using System.Collections.Generic;
using System.Threading;
using System.Linq;
using wManager.Wow.Helpers;
using System.Globalization;
using wManager.Wow.ObjectManager;

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
    private Dictionary<string, float> ItemStats { get; set; } = new Dictionary<string, float>(){};
    public static Dictionary<string, int> StatsWeights { get; set; } = new Dictionary<string, int>() {
        {"Stamina", AutoEquipSettings.CurrentSettings.StaminaWeight},
        {"Intellect", AutoEquipSettings.CurrentSettings.IntellectWeight},
        {"Agility", AutoEquipSettings.CurrentSettings.AgilityWeight},
        {"Strength", AutoEquipSettings.CurrentSettings.StrengthWeight},
        {"Spirit", AutoEquipSettings.CurrentSettings.SpiritWeight},
        {"Armor", AutoEquipSettings.CurrentSettings.ArmorWeight},
        {"Attack Power", AutoEquipSettings.CurrentSettings.AttackPowerWeight},
        {"Hit Rating", AutoEquipSettings.CurrentSettings.HitRatingWeight},
        {"Mana Per 5 Sec.", AutoEquipSettings.CurrentSettings.Mana5Weight},
        {"Damage Per Second", AutoEquipSettings.CurrentSettings.DPSWeight},
        {"Block Value", AutoEquipSettings.CurrentSettings.ShieldBlockWeight},
        {"Block Rating", AutoEquipSettings.CurrentSettings.ShieldBlockRatingWeight},
        {"Defense Rating", AutoEquipSettings.CurrentSettings.DefenseRatingWeight},
        {"Spell Power", AutoEquipSettings.CurrentSettings.SpellPowerWeight},
        {"Dodge Rating", AutoEquipSettings.CurrentSettings.DodgeRatingWeight},
        {"Critical Strike Rating", AutoEquipSettings.CurrentSettings.CritRatingWeight},
        {"Expertise Rating", AutoEquipSettings.CurrentSettings.ExpertiseRatingWeight},
        {"Haste Rating", AutoEquipSettings.CurrentSettings.HasteRatingWeight},
        {"Armor Penetration Rating", AutoEquipSettings.CurrentSettings.ArmorPenetrationWeight},
        {"Parry Rating", AutoEquipSettings.CurrentSettings.ParryRatingWeight},
        {"Resilience Rating", AutoEquipSettings.CurrentSettings.ResilienceWeight},
        {"Spell Penetration", AutoEquipSettings.CurrentSettings.SpellPenetrationWeight},
        {"Attack Power In Forms", AutoEquipSettings.CurrentSettings.FeralAttackPower}
    };

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
            itemSubType, itemStackCount, itemEquipLoc, itemTexture, itemSellPrice = GetItemInfo(""{ItemLink}"");

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
    }

    public void RecordStats()
    {
        if (ItemType != "Armor" && ItemType != "Weapon")
            return;

        string stats = Lua.LuaDoString<string>($@"local itemstats=GetItemStats(""{ItemLink}"") 
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
                ItemStats.Add(statName, statValue);
            }
        }
        RecordWeightScore();
    }

    private void RecordWeightScore()
    {
        //Logger.Log(Name);
        foreach (KeyValuePair<string, float> entry in ItemStats)
        {
            if (StatsWeights.ContainsKey(entry.Key) && StatsWeights[entry.Key] != 0)
            {
                WeightScore += entry.Value * StatsWeights[entry.Key];
                //Logger.Log(entry.Key + " -> " + (entry.Value * StatsWeights[entry.Key]).ToString());
            }
            else
            {
                if (!entry.Key.Contains("Socket"))
                    Logger.LogError("Can't detect : " + entry.Key);
            }
        }
        //Logger.Log("Total : " + WeightScore.ToString()); ;
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
                if (l.Contains(" Slot Bag"))
                    BagCapacity = int.Parse(l.Replace(" Slot Bag", ""));
                else if (l.Contains(" Slot Quiver"))
                    QuiverCapacity = int.Parse(l.Replace(" Slot Quiver", ""));
                else if (l.Contains(" Slot Ammo Pouch"))
                    AmmoPouchCapacity = int.Parse(l.Replace(" Slot Ammo Pouch", ""));
            }
        }
    }

    public void Use()
    {
        if (InBag < 0 || InBagSlot < 0)
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        else
            Lua.LuaDoString($"UseContainerItem({InBag}, {InBagSlot})");
    }

    public bool Equip(int slotId)
    {
        if (InBag < 0 || InBagSlot < 0)
        {
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        }
        else
        {
            ItemEquipAttempts.Add(ItemLink);
            PickupFromBag();
            DropInInventory(slotId);
            Thread.Sleep(100);
            Lua.LuaDoString($"EquipPendingItem(0);");
            Thread.Sleep(200);
            WAECharacterSheet.Scan();
            WAEBagInventory.Scan();
            WAECharacterSheetSlot slot = WAECharacterSheet.AllSlots.Find(s => s.InventorySlotID == slotId);
            if (slot.Item == null || slot.Item.ItemLink != ItemLink)
            {
                Logger.LogError($"Failed to equip {Name}. Retrying soon ({GetNbEquipAttempts()}).");
                Lua.LuaDoString($"ClearCursor()");
                return false;
            }
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
        WoWLocalPlayer me = ObjectManager.Me;
        return me.Level >= ItemMinLevel;
    }

    public bool MoveToBag(int position, int slot)
    {
        Lua.LuaDoString($"PickupContainerItem({position}, {slot});"); // en fait un clique sur le slot de destination
        Thread.Sleep(200);
        if (WAEBagInventory.ListContainers.Find(bag => bag.Position == position).GetContainerItemlink(slot) == ItemLink)
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
        Logger.Log($"Name : {Name}");
        Logger.Log($"ItemLink : {ItemLink}");
        Logger.Log($"ItemRarity : {ItemRarity}");
        Logger.Log($"ItemLevel : {ItemLevel}");
        Logger.Log($"ItemMinLevel : {ItemMinLevel}");
        Logger.Log($"ItemType : {ItemType}");
        Logger.Log($"ItemSubType : {ItemSubType}");
        Logger.Log($"ItemStackCount : {ItemStackCount}");
        Logger.Log($"ItemEquipLoc : {ItemEquipLoc}");
        Logger.Log($"ItemTexture : {ItemTexture}");
        Logger.Log($"ItemSellPrice : {ItemSellPrice}");
        Logger.Log($"QuiverCapacity : {QuiverCapacity}");
        Logger.Log($"AmmoPouchCapacity : {AmmoPouchCapacity}");
        Logger.Log($"BagCapacity : {BagCapacity}");
        Logger.Log($"UniqueId : {UniqueId}");
    }
}