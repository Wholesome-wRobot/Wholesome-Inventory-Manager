using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using wManager.Wow.ObjectManager;

namespace Wholesome_Inventory_Manager.Managers.Roll
{
    internal class LootPriorityCoordinator
    {
        private const int IntentMaxAgeSeconds = 60;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        private string CacheDirectory => Path.Combine(
            Others.GetCurrentDirectory,
            "Settings",
            "WholesomeInventoryManager",
            "LootRollCache");

        public bool TryWriteIntent(int rollId, LootPriorityRole role)
        {
            try
            {
                Directory.CreateDirectory(CacheDirectory);
                CleanupOldIntents();

                LootRollIntent intent = new LootRollIntent
                {
                    RollId = rollId,
                    Role = role,
                    CreatedAtUtc = DateTime.UtcNow
                };

                string fileName = $"{SafeFileName(ObjectManager.Me.Name)}_{rollId}.json";
                string finalPath = Path.Combine(CacheDirectory, fileName);
                string tempPath = finalPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

                File.WriteAllText(tempPath, _serializer.Serialize(intent));

                if (File.Exists(finalPath))
                    File.Delete(finalPath);

                File.Move(tempPath, finalPath);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("LootPriorityCoordinator > TryWriteIntent(): " + e.Message);
                return false;
            }
        }

        public bool SomeoneHasHigherPriorityNeed(int rollId, LootPriorityRole currentRole)
        {
            try
            {
                CleanupOldIntents();

                return ReadValidIntents(rollId)
                    .Any(intent => intent.Role > currentRole);
            }
            catch (Exception e)
            {
                Logger.LogError("LootPriorityCoordinator > SomeoneHasHigherPriorityNeed(): " + e.Message);
                return false;
            }
        }

        private IEnumerable<LootRollIntent> ReadValidIntents(int rollId)
        {
            if (!Directory.Exists(CacheDirectory))
                return Enumerable.Empty<LootRollIntent>();

            DateTime oldestValidDate = DateTime.UtcNow.AddSeconds(-IntentMaxAgeSeconds);
            List<LootRollIntent> intents = new List<LootRollIntent>();

            foreach (string filePath in Directory.GetFiles(CacheDirectory, "*.json"))
            {
                LootRollIntent intent = ReadIntent(filePath);
                if (intent == null
                    || intent.CreatedAtUtc < oldestValidDate
                    || intent.RollId != rollId)
                {
                    continue;
                }

                intents.Add(intent);
            }

            return intents;
        }

        private LootRollIntent ReadIntent(string filePath)
        {
            try
            {
                return _serializer.Deserialize<LootRollIntent>(File.ReadAllText(filePath));
            }
            catch (Exception e)
            {
                Logger.LogError("LootPriorityCoordinator > ReadIntent(): " + e.Message);
                return null;
            }
        }

        private void CleanupOldIntents()
        {
            if (!Directory.Exists(CacheDirectory))
                return;

            DateTime oldestValidDate = DateTime.UtcNow.AddSeconds(-IntentMaxAgeSeconds);

            foreach (string filePath in Directory.GetFiles(CacheDirectory, "*.json"))
            {
                LootRollIntent intent = ReadIntent(filePath);
                if (intent == null || intent.CreatedAtUtc < oldestValidDate)
                    TryDelete(filePath);
            }

            foreach (string filePath in Directory.GetFiles(CacheDirectory, "*.tmp"))
            {
                TryDelete(filePath);
            }
        }

        private void TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Logger.LogError("LootPriorityCoordinator > TryDelete(): " + e.Message);
            }
        }

        private string SafeFileName(string value)
        {
            string safe = Regex.Replace(value ?? "Unknown", @"[^\w\.-]", "_");
            return string.IsNullOrEmpty(safe) ? "Unknown" : safe;
        }
    }
}
