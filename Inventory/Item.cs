using System.Threading;
using wManager.Wow.Helpers;

public class Item
{
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
    public int IsInBagSlot { get; set; } = -1;
    public int IsInBag { get; set; } = -1;
    public int UniqueId { get; set; }

    private static int UniqueIdCounter = 0;

    public Item(string itemLink)
    {
        ItemLink = itemLink;
        UniqueId = ++UniqueIdCounter;
        
        Item existingCopy = SessionItemDB.Get(ItemLink);
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
            SessionItemDB.Add(this);
        }
    }

    private void CloneFromDB(Item existingCopy)
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
    }

    public string GetItemStats()
    {
        string stats = Lua.LuaDoString<string>($@"local itemstats=GetItemStats(""{ItemLink}"") 
                        local stats = """" for stat, value in pairs(itemstats) do stats = stats.._G[stat].."":""..value.."";"" end
                        return stats");
        return stats;
    }

    public void RecordToolTip()
    {
        // Record the info present in the tooltip
        string lines = Lua.LuaDoString<string>($@"
            WEquipTooltip:ClearLines()
            WEquipTooltip:SetHyperlink(""{ItemLink}"")
            return EnumerateTooltipLines(WEquipTooltip: GetRegions())");
        string[] allLines =  lines.Split('|');
        foreach(string l in allLines)
        {
            if (l.Length > 0)
            {
                if (l.Contains(" Slot Bag"))
                    BagCapacity = int.Parse(l.Replace(" Slot Bag", ""));
                if (l.Contains(" Slot Quiver"))
                    QuiverCapacity = int.Parse(l.Replace(" Slot Quiver", ""));
            }
        }
    }

    public void Use()
    {
        if (IsInBag < 0 || IsInBagSlot < 0)
            Logger.LogError($"Item {Name} is not recorded as being in a bag. Can't use.");
        else
            Lua.LuaDoString($"UseContainerItem({IsInBag}, {IsInBagSlot})");
    }

    public void Pickup()
    {
        Lua.LuaDoString($"ClearCursor(); PickupContainerItem({IsInBag}, {IsInBagSlot});");
    }

    public bool DropInBag(int position, int slot)
    {
        Lua.LuaDoString($"PickupContainerItem({position}, {slot});"); // en fait un clique sur le slot de destination
        Thread.Sleep(200);
        if (Main.ListBags.Find(bag => bag.Position == position).GetContainerItemlink(slot) == ItemLink)
            return true;
        Logger.LogError($"Couldn't move {Name} to bag {position} slot {slot}, retrying soon.");
        return false;
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
    }
}