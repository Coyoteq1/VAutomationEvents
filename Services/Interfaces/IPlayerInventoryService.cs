using System.Collections.Generic;

namespace VAuto.Services.Interfaces
{
    public interface IPlayerInventoryService
    {
        // Attempt to equip an item for a player; return true on success
        bool EquipItem(string playerId, string itemName, int count);

        // Get currently equipped items for a player
        IEnumerable<EquippedItem> GetEquippedItems(string playerId);

        // Clear currently equipped items (used before restore)
        void ClearEquippedItems(string playerId);
    }

    public class EquippedItem
    {
        public string ItemName { get; set; }
        public int Count { get; set; }
    }
}