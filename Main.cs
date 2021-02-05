using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using wManager.Plugin;
using wManager.Wow.Helpers;
using System.Collections.Generic;
using MoreLinq;

public class Main : IPlugin
{
    public static bool isLaunched;
    private readonly BackgroundWorker detectionPulse = new BackgroundWorker();

    public static List<Container> ListBags { get; set; } = new List<Container>();

    public void Initialize()
    {
        isLaunched = true;

        AutoEquipSettings.Load();

        detectionPulse.DoWork += BackGroundPulse;
        detectionPulse.RunWorkerAsync();

        Setup();
    }

    public void Dispose()
    {
        detectionPulse.DoWork -= BackGroundPulse;
        detectionPulse.Dispose();
        Logger.Log("Disposed");
        isLaunched = false;
    }

    private void BackGroundPulse(object sender, DoWorkEventArgs args)
    {
        while (isLaunched)
        {
            try
            {
                DateTime dateBegin = DateTime.Now;
                Logger.Log("Scanning bags...");
                ScanBags();
                Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");

                dateBegin = DateTime.Now;
                Logger.Log("Checking bag equip...");
                BagEquip();
                Logger.Log($"Process time : {(DateTime.Now.Ticks - dateBegin.Ticks) / 10000} ms");
            }
            catch (Exception arg)
            {
                Logger.LogError(string.Concat(arg));
            }
            Thread.Sleep(3000);
        }
    }

    public void Settings()
    {
        AutoEquipSettings.Load();
        AutoEquipSettings.CurrentSettings.ToForm();
        AutoEquipSettings.CurrentSettings.Save();
    }

    private void ScanBags()
    {
        ListBags.Clear();
        Container.AllItemsInAllBags.Clear();
        for (int i = 0; i < 5; i++)
        {
            string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
            if (!bagName.Equals(""))
                ListBags.Add(new Container(i));
        }
    }

    private void BagEquip()
    {
        int maxAmountOfBags = 5;
        if (ListBags.Count < maxAmountOfBags)
        {
            List<int> emptyContainerSlots = Container.GetEmptyContainerSlots();
            int nbEmpty = emptyContainerSlots.Count;
            int nbloop = emptyContainerSlots.Count;

            // Bag equip if we have at least 1 empty slot
            foreach (int emptySlotId in emptyContainerSlots)
            {
                Item biggestBag = Container.AllItemsInAllBags.OrderByDescending(item => item.BagCapacity).FirstOrDefault();
                if (biggestBag != null)
                {
                    Logger.Log($"Equipping {biggestBag.Name}");
                    biggestBag.Use();
                    Thread.Sleep(200);
                    if (Container.GetEmptyContainerSlots().Count < nbEmpty)
                    {
                        nbEmpty--;
                        ListBags.Add(new Container(emptySlotId));
                        Container.AllItemsInAllBags.Remove(biggestBag);
                    }
                }
            }
        }

        // Bag equip to replace one for better capacity // should probably be done last because it fucks up the model
        if (ListBags.Count >= maxAmountOfBags)
        {
            Container smallestEquippedBag = ListBags.OrderBy(bag => bag.Capacity).FirstOrDefault();
            Item biggestBagInBags = Container.AllItemsInAllBags.OrderByDescending(item => item.BagCapacity).FirstOrDefault();
            if (smallestEquippedBag.Capacity < biggestBagInBags.BagCapacity)
            {
                Logger.Log($"Replacing {smallestEquippedBag.GetContainerName()} by {biggestBagInBags.Name}");
                int nbItemsInSmallEquippedBag = smallestEquippedBag.Items.Count;
                List<ContainerSlot> freeSlots = new List<ContainerSlot>();

                foreach(Container container in ListBags)
                {
                    if (container != smallestEquippedBag)
                        freeSlots.AddRange(container.Slots.Where(slot => slot.OccupiedBy == null));
                }

                if (freeSlots.Count > smallestEquippedBag.Items.Count)
                {
                    for (int i = 0; i < smallestEquippedBag.Items.Count; i++)
                    {
                        ContainerSlot destination = freeSlots[i];
                        Item smallBag = smallestEquippedBag.Items[i];
                        smallBag.Pickup();
                        Thread.Sleep(50);
                        smallBag.DropInBag(destination.BagPosition, destination.Slot);
                    }
                }

                // Check if bag to move is actually empty
                if (smallestEquippedBag.GetContainerNbFreeSlots() == smallestEquippedBag.GetContainerNbSlots())
                {
                    biggestBagInBags.Pickup();
                    Thread.Sleep(50);
                    int bagSlot = 19 + smallestEquippedBag.Position;
                    Lua.LuaDoString($"PutItemInBag({bagSlot})");
                }
            }
        }
    }

    private void Setup()
    {
        // Create invisible tooltip to read tooltip info
        Lua.LuaDoString($@"
            local tip = myTooltip or CreateFrame(""GAMETOOLTIP"", ""WEquipTooltip"")
            local L = L or tip: CreateFontString()
            local R = R or tip: CreateFontString()
            L: SetFontObject(GameFontNormal)
            R: SetFontObject(GameFontNormal)
            WEquipTooltip: AddFontStrings(L, R)
            WEquipTooltip: SetOwner(WorldFrame, ""ANCHOR_NONE"")");

        // Create function to read invisible tooltip lines
        Lua.LuaDoString($@"
            function EnumerateTooltipLines(...)
                local result = """"
                for i = 1, select(""#"", ...) do
                    local region = select(i, ...)
                    if region and region:GetObjectType() == ""FontString"" then
                        local text = region:GetText() or """"
                        if text ~= """" then
                            result = result .. ""|"" .. text
                        end
                    end
                end
                return result
            end");
    }
}