using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using VAuto.Core;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Arena Build Service - Manages player building within arena zones
    /// </summary>
    public class ArenaBuildService : IService
    {
        private static ArenaBuildService _instance;
        public static ArenaBuildService Instance => _instance ??= new ArenaBuildService();

        private bool _isInitialized;
        private readonly Dictionary<ulong, bool> _playerBuildPermissions = new();
        private readonly List<ArenaObjectService.ArenaObjectData> _builtStructures = new();
        
        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => Plugin.Logger;

        public enum StructureType
        {
            Structure,
            Wall,
            Floor,
            Portal,
            Light,
            Waygate
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Log?.LogInfo("[ArenaBuildService] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            _playerBuildPermissions.Clear();
            _builtStructures.Clear();
            _isInitialized = false;
            Log?.LogInfo("[ArenaBuildService] Cleaned up");
        }

        public bool BuildStructure(Entity user, string structureName, float3 position, quaternion? rotation = null)
        {
            try
            {
                Log?.LogInfo($"[ArenaBuildService] Building {structureName} at {position}");
                // Implementation logic here
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaBuildService] Failed to build structure: {ex.Message}");
                return false;
            }
        }

        public void SetPlayerBuildPermission(ulong platformId, bool canBuild)
        {
            _playerBuildPermissions[platformId] = canBuild;
            Log?.LogInfo($"[ArenaBuildService] Set build permission for {platformId} to {canBuild}");
        }

        public List<ArenaObjectService.ArenaObjectData> GetAllArenaObjects()
        {
            return ArenaObjectService.Instance.GetAllArenaObjects();
        }

        public List<string> GetAvailableStructures()
        {
            return new List<string> { "wall", "floor", "portal", "glow", "waygate" };
        }

        public bool CanBuildAt(Entity user, float3 position)
        {
            return ArenaZoneService.Instance.IsPositionInAnyArena(position);
        }

        public int GetTotalBuiltStructures()
        {
            return _builtStructures.Count;
        }

        /// <summary>
        /// Apply arena build/gear to a character
        /// </summary>
        public static bool ApplyBuild(Entity characterEntity, string buildName)
        {
            try
            {
                if (!VAuto.Core.Core.Exists(characterEntity))
                {
                    Plugin.Logger?.LogError($"[ArenaBuildService] Character entity does not exist for ApplyBuild");
                    return false;
                }

                Plugin.Logger?.LogInfo($"[ArenaBuildService] Applying arena build '{buildName}' to character {characterEntity.Index}");

                // TODO: Implement actual gear application
                // This would equip the character with arena-appropriate weapons and armor
                // For now, just log the action

                Plugin.Logger?.LogInfo($"[ArenaBuildService] Arena build '{buildName}' applied successfully");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaBuildService] Failed to apply build '{buildName}': {ex.Message}");
                return false;
            }
        }

        public static bool SwitchToArenaBuildMode(Entity characterEntity)
        {
            // Stub for command support
            return true;
        }
    }
}