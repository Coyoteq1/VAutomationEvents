using ProjectM;
using ProjectM.CastleBuilding;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM.Gameplay.Scripting;
using VAuto.Extensions;

namespace VAuto.Data
{
    /// <summary>
    /// Tile and prefab management system
    /// </summary>
    internal static class Tile
    {
        public static Dictionary<string, PrefabGUID> Named = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<int, string> NameFromPrefab = new Dictionary<int, string>();
        public static Dictionary<string, PrefabGUID> LowerCaseNameToPrefab = new Dictionary<string, PrefabGUID>();
        public static HashSet<PrefabGUID> ValidPrefabsForBuilding = new HashSet<PrefabGUID>();

        /// <summary>
        /// Populate tile system with valid building prefabs
        /// </summary>
        public static void Populate()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Plugin.Log?.LogInfo($"[{timestamp}] TILE_POPULATE_START - Beginning tile system population");
            
            try
            {
                // TODO: Implement tile population when PrefabCollection access is fixed
                Plugin.Log?.LogInfo($"[{timestamp}] TILE_POPULATE_SKIP - PrefabCollection access issues, tile population disabled");
                Plugin.Log?.LogInfo($"[{timestamp}] TILE_POPULATE_COMPLETE - Tile system populated with 0 prefabs (disabled mode)");
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogError($"[{timestamp}] TILE_POPULATE_ERROR - Failed to populate tile system: {ex.Message}");
                Plugin.Log?.LogError($"[{timestamp}] TILE_POPULATE_STACK_TRACE - {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Check if a prefab is valid for building
        /// </summary>
        public static bool IsValidForBuilding(PrefabGUID prefabGuid)
        {
            return ValidPrefabsForBuilding.Contains(prefabGuid);
        }

        /// <summary>
        /// Get prefab GUID by name
        /// </summary>
        public static bool GetPrefabByName(string name, out PrefabGUID prefabGuid)
        {
            return LowerCaseNameToPrefab.TryGetValue(name.ToLower(), out prefabGuid);
        }

        /// <summary>
        /// Get prefab name by GUID
        /// </summary>
        public static string GetPrefabName(PrefabGUID prefabGuid)
        {
            return NameFromPrefab.TryGetValue(prefabGuid.GuidHash, out var name) ? name : "Unknown";
        }

        /// <summary>
        /// Clear all tile data
        /// </summary>
        public static void Clear()
        {
            Named.Clear();
            NameFromPrefab.Clear();
            LowerCaseNameToPrefab.Clear();
            ValidPrefabsForBuilding.Clear();
        }
    }
}
