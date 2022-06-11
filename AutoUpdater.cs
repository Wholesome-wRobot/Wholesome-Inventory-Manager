using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Net;
using System.Text;

public static class AutoUpdater
{
    public static bool CheckUpdate(string mainVersion)
    {
        if (wManager.Information.Version.Contains("1.7.2"))
        {
            Logger.Log($"Plugin couldn't load (v {wManager.Information.Version})");
            Products.ProductStop();
            return false;
        }

        Version currentVersion = new Version(mainVersion);

        DateTime dateBegin = new DateTime(2020, 1, 1);
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - dateBegin.Ticks;
        elapsedTicks /= 10000000;

        double timeSinceLastUpdate = elapsedTicks - AutoEquipSettings.CurrentSettings.LastUpdateDate;

        // If last update try was < 10 seconds ago, we exit to avoid looping
        if (timeSinceLastUpdate < 30)
        {
            Logger.Log($"Last update attempts was {timeSinceLastUpdate} seconds ago. Exiting updater.");
            return false;
        }

        try
        {
            AutoEquipSettings.CurrentSettings.LastUpdateDate = elapsedTicks;
            AutoEquipSettings.CurrentSettings.Save();

            string onlineDllLink = "https://github.com/Wholesome-wRobot/Wholesome_Inventory_Manager/raw/master/Compiled/Wholesome_Inventory_Manager.dll";
            string onlineVersionLink = "https://raw.githubusercontent.com/Wholesome-wRobot/Wholesome_Inventory_Manager/master/Compiled/Version.txt";

            var onlineVersionTxt = new WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersionLink);
            Version onlineVersion = new Version(onlineVersionTxt);

            if (onlineVersion.CompareTo(currentVersion) <= 0)
            {
                Logger.Log($"Your version is up to date ({currentVersion} / {onlineVersion})");
                return false;
            }

            // File check
            string currentFile = Others.GetCurrentDirectory + @"\Plugins\Wholesome_Inventory_Manager.dll";
            var onlineFileContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineDllLink);
            if (onlineFileContent != null && onlineFileContent.Length > 0)
            {
                Logger.Log($"Updating your version {currentVersion} to online Version {onlineVersion}");
                System.IO.File.WriteAllBytes(currentFile, onlineFileContent); // replace user file by online file
                ToolBox.Sleep(1000);
                return true;
            }
        }
        catch (Exception e)
        {
            Logging.WriteError("Auto update: " + e);
        }
        return false;
    }
}
