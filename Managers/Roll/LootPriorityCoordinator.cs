using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public bool TryWriteIntent(int rollId, LootPriority priority)
        {
            try
            {
                Directory.CreateDirectory(CacheDirectory);
                CleanupOldIntents();

                LootRollIntent intent = new LootRollIntent
                {
                    RollId = rollId,
                    Priority = priority,
                    CreatedAtUtc = DateTime.UtcNow
                };

                string fileName = $"{ToolBox.SafeFileName(ObjectManager.Me.Name)}_{rollId}.json";
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

        public bool SomeoneHasHigherLootPriority(int rollId, LootPriority currentPriority)
        {
            try
            {
                CleanupOldIntents();

                return ReadValidIntents(rollId)
                    .Any(intent => intent.Priority > currentPriority);
            }
            catch (Exception e)
            {
                Logger.LogError("LootPriorityCoordinator > SomeoneHasHigherLootPriority(): " + e.Message);
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
                    ToolBox.TryDelete(filePath);
            }

            foreach (string filePath in Directory.GetFiles(CacheDirectory, "*.tmp"))
            {
                ToolBox.TryDelete(filePath);
            }
        }
    }
}
