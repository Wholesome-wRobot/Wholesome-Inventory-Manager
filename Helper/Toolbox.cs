using robotManager.Products;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Helpers;

public class ToolBox
{
    public static void Restart()
    {
        new Thread(() =>
        {
            Products.ProductStop();
            ToolBox.Sleep(2000);
            Products.ProductStart();
        }).Start();
    }

    public static string GetWoWVersion()
    {
        return Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
    }

    public static int GetTalentRank(int tabIndex, int talentIndex)
    {
        int rank = Lua.LuaDoString<int>($"local _, _, _, _, currentRank, _, _, _ = GetTalentInfo({tabIndex}, {talentIndex}); return currentRank;");
        return rank;
    }


    // Gets Character's specialization (by Marsbar) Modified to return 0 if all talent trees have 0 point
    public static int GetSpec()
    {
        var Talents = new Dictionary<int, int>();
        for (int i = 0; i <= 3; i++)
        {
            Talents.Add(
                i,
                Lua.LuaDoString<int>($"local name, iconTexture, pointsSpent = GetTalentTabInfo({i}); return pointsSpent")
            );
        }
        int highestTalents = Talents.Max(x => x.Value);
        return Talents.Where(t => t.Value == highestTalents).FirstOrDefault().Key;
    }

    public static void Sleep(int milliseconds)
    {
        int worldLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagWorld");
        int homeLatency = Lua.LuaDoString<int>($"local down, up, lagHome, lagWorld = GetNetStats(); return lagHome");
        Thread.Sleep(worldLatency + homeLatency + milliseconds);
    }
}