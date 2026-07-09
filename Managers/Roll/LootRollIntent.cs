using System;

namespace Wholesome_Inventory_Manager.Managers.Roll
{
    public class LootRollIntent
    {
        public int RollId { get; set; }
        public int ItemId { get; set; }
        public string CharacterName { get; set; }
        public LootPriorityRole Role { get; set; }
        public bool WantsNeed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
