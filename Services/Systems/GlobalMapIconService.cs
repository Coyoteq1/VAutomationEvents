using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Global Map Icon Service - Updates map icons every 3 seconds for all players
    /// Uses PlayerLocationTracker for player positions and MapIcon_CastleObject_BloodAltar prefab
    /// </summary>
    public static class GlobalMapIconService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, MapIconData> _playerIcons = new();
        private static readonly Dictionary<string, MapIconPrefabConfig> _iconPrefabs = new();
        private static readonly object _lock = new object();
        private static System.Timers.Timer _updateTimer;
        private static float _updateInterval = 3.0f; // 3 seconds
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;
        public static string MapIconPrefabName => "MapIcon_CastleObject_BloodAltar";

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[GlobalMapIconService] Initializing global map icon service...");
                    
                    _playerIcons.Clear();
                    InitializeIconPrefabs();
                    
                    // Start update timer (every 3 seconds)
                    _updateTimer = new System.Timers.Timer(_updateInterval * 1000);
                    _updateTimer.Elapsed += OnUpdateTimerElapsed;
                    _updateTimer.AutoReset = true;
                    _updateTimer.Start();
                    
                    _initialized = true;
                    
                    Log?.LogInfo($"[GlobalMapIconService] Global map icon service initialized with {MapIconPrefabName} prefab, 3-second updates");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[GlobalMapIconService] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    Log?.LogInfo("[GlobalMapIconService] Cleaning up global map icon service...");
                    
                    // Stop update timer
                    _updateTimer?.Stop();
                    _updateTimer?.Dispose();
                    _updateTimer = null;
                    
                    // Clear all map icons
                    ClearAllMapIcons();
                    
                    _playerIcons.Clear();
                    _iconPrefabs.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[GlobalMapIconService] Global map icon service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[GlobalMapIconService] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void InitializeIconPrefabs()
        {
            try
            {
                // Configure map icon prefabs
                _iconPrefabs["player_default"] = new MapIconPrefabConfig
                {
                    PrefabName = MapIconPrefabName,
                    IconType = "Player",
                    DefaultColor = new float4(0.2f, 0.8f, 1.0f, 1.0f), // Cyan
                    Scale = 1.0f,
                    IsVisible = true
                };

                _iconPrefabs["player_arena"] = new MapIconPrefabConfig
                {
                    PrefabName = MapIconPrefabName,
                    IconType = "PlayerInArena",
                    DefaultColor = new float4(1.0f, 0.5f, 0.0f, 1.0f), // Orange
                    Scale = 1.2f,
                    IsVisible = true
                };

                _iconPrefabs["player_pvp"] = new MapIconPrefabConfig
                {
                    PrefabName = MapIconPrefabName,
                    IconType = "PlayerInPvP",
                    DefaultColor = new float4(1.0f, 0.2f, 0.2f, 1.0f), // Red
                    Scale = 1.5f,
                    IsVisible = true
                };

                Log?.LogInfo($"[GlobalMapIconService] Initialized {_iconPrefabs.Count} icon prefab configurations");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Failed to initialize icon prefabs: {ex.Message}");
            }
        }
        #endregion

        #region Update Timer
        private static void OnUpdateTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!_initialized) return;
                
                // Update all map icons
                UpdateAllPlayerIcons();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error in update timer: {ex.Message}");
            }
        }
        #endregion

        #region Map Icon Management
        public static void UpdateAllPlayerIcons()
        {
            try
            {
                lock (_lock)
                {
                    // Get all online players
                    var onlinePlayers = GetAllOnlinePlayers();
                    
                    foreach (var player in onlinePlayers)
                    {
                        UpdatePlayerIcon(player);
                    }
                    
                    // Remove icons for players who went offline
                    RemoveOfflinePlayerIcons(onlinePlayers);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error updating player icons: {ex.Message}");
            }
        }

        private static void UpdatePlayerIcon(UserData player)
        {
            try
            {
                if (player.CharacterEntity == Entity.Null)
                    return;

                var platformId = player.PlatformId;
                var position = GetEntityPosition(player.CharacterEntity);
                var rotation = GetEntityRotation(player.CharacterEntity);
                
                // Determine icon type based on player state
                var iconType = GetIconTypeForPlayer(player, position);
                var iconConfig = GetIconConfig(iconType);
                
                if (!_playerIcons.ContainsKey(platformId))
                {
                    // Create new map icon
                    CreateMapIcon(player, position, rotation, iconConfig);
                }
                else
                {
                    // Update existing map icon
                    UpdateMapIcon(platformId, position, rotation, iconConfig);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error updating player icon for {player.CharacterName}: {ex.Message}");
            }
        }

        private static void CreateMapIcon(UserData player, float3 position, quaternion rotation, MapIconPrefabConfig config)
        {
            try
            {
                var platformId = player.PlatformId;
                
                // Create map icon entity using the specified prefab
                var iconEntity = CreateMapIconEntity(position, rotation, config);
                
                if (iconEntity != Entity.Null)
                {
                    _playerIcons[platformId] = new MapIconData
                    {
                        PlatformId = platformId,
                        CharacterName = player.CharacterName,
                        IconEntity = iconEntity,
                        Position = position,
                        Rotation = rotation,
                        IconType = config.IconType,
                        Color = config.DefaultColor,
                        Scale = config.Scale,
                        LastUpdate = DateTime.UtcNow,
                        IsVisible = config.IsVisible
                    };
                    
                    Log?.LogDebug($"[GlobalMapIconService] Created map icon for {player.CharacterName} at {position}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error creating map icon for {player.CharacterName}: {ex.Message}");
            }
        }

        private static void UpdateMapIcon(ulong platformId, float3 position, quaternion rotation, MapIconPrefabConfig config)
        {
            try
            {
                var iconData = _playerIcons[platformId];
                
                // Update position and rotation
                iconData.Position = position;
                iconData.Rotation = rotation;
                iconData.LastUpdate = DateTime.UtcNow;
                
                // Update visual properties if needed
                if (iconData.IconType != config.IconType || !iconData.Color.Equals(config.DefaultColor))
                {
                    iconData.IconType = config.IconType;
                    iconData.Color = config.DefaultColor;
                    iconData.Scale = config.Scale;
                    
                    // Update the entity visual properties
                    UpdateMapIconVisual(iconData.IconEntity, position, rotation, config);
                }
                
                Log?.LogDebug($"[GlobalMapIconService] Updated map icon for {platformId} at {position}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error updating map icon for {platformId}: {ex.Message}");
            }
        }

        private static void RemoveOfflinePlayerIcons(List<UserData> onlinePlayers)
        {
            try
            {
                var onlinePlatformIds = onlinePlayers.Select(p => p.PlatformId).ToHashSet();
                var iconsToRemove = _playerIcons.Keys.Where(id => !onlinePlatformIds.Contains(id)).ToList();
                
                foreach (var platformId in iconsToRemove)
                {
                    RemoveMapIcon(platformId);
                }
                
                if (iconsToRemove.Count > 0)
                {
                    Log?.LogDebug($"[GlobalMapIconService] Removed {iconsToRemove.Count} offline player icons");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapService] Error removing offline player icons: {ex.Message}");
            }
        }

        private static void RemoveMapIcon(ulong platformId)
        {
            try
            {
                if (_playerIcons.TryGetValue(platformId, out var iconData))
                {
                    // Destroy the map icon entity
                    if (iconData.IconEntity != Entity.Null)
                    {
                        var em = VAutoCore.EntityManager;
                        if (em.Exists(iconData.IconEntity))
                        {
                            em.DestroyEntity(iconData.IconEntity);
                        }
                    }
                    
                    _playerIcons.Remove(platformId);
                    
                    Log?.LogDebug($"[GlobalMapIconService] Removed map icon for {platformId}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error removing map icon for {platformId}: {ex.Message}");
            }
        }

        private static void ClearAllMapIcons()
        {
            try
            {
                foreach (var iconData in _playerIcons.Values)
                {
                    if (iconData.IconEntity != Entity.Null)
                    {
                        var em = VAutoCore.EntityManager;
                        if (em.Exists(iconData.IconEntity))
                        {
                            em.DestroyEntity(iconData.IconEntity);
                        }
                    }
                }
                
                Log?.LogDebug($"[GlobalMapIconService] Cleared {_playerIcons.Count} map icons");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error clearing map icons: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        private static Entity CreateMapIconEntity(float3 position, quaternion rotation, MapIconPrefabConfig config)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var iconEntity = em.CreateEntity();
                
                // Add transform components
                em.AddComponentData(iconEntity, new Translation { Value = position });
                em.AddComponentData(iconEntity, new Rotation { Value = rotation });
                em.AddComponentData(iconEntity, new NonUniformScale { Value = new float3(config.Scale) });
                
                // Add map icon specific components
                AddMapIconComponents(iconEntity, config);
                
                // Add prefab reference (using the specified prefab)
                AddPrefabReference(iconEntity, config.PrefabName);
                
                return iconEntity;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Failed to create map icon entity: {ex.Message}");
                return Entity.Null;
            }
        }

        private static void AddMapIconComponents(Entity iconEntity, MapIconPrefabConfig config)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Add map icon data component
                em.AddComponentData(iconEntity, new MapIconComponent
                {
                    IconType = config.IconType,
                    Color = config.DefaultColor,
                    Scale = config.Scale,
                    IsVisible = config.IsVisible
                });
                
                // Add any other required components for map icons
                // This would depend on the specific map icon system in V Rising
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Failed to add map icon components: {ex.Message}");
            }
        }

        private static void AddPrefabReference(Entity iconEntity, string prefabName)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Add prefab reference component
                // This would be the specific component used for prefab instantiation in V Rising
                // For now, we'll add a placeholder
                em.AddComponentData(iconEntity, new PrefabReferenceComponent
                {
                    PrefabName = prefabName
                });
                
                Log?.LogDebug($"[GlobalMapIconService] Added prefab reference: {prefabName}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Failed to add prefab reference: {ex.Message}");
            }
        }

        private static void UpdateMapIconVisual(Entity iconEntity, float3 position, quaternion rotation, MapIconPrefabConfig config)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Update transform
                if (em.HasComponent<Translation>(iconEntity))
                {
                    em.SetComponentData(iconEntity, new Translation { Value = position });
                }
                
                if (em.HasComponent<Rotation>(iconEntity))
                {
                    em.SetComponentData(iconEntity, new Rotation { Value = rotation });
                }
                
                if (em.HasComponent<NonUniformScale>(iconEntity))
                {
                    em.SetComponentData(iconEntity, new NonUniformScale { Value = new float3(config.Scale) });
                }
                
                // Update visual properties
                if (em.HasComponent<MapIconComponent>(iconEntity))
                {
                    var iconComponent = em.GetComponentData<MapIconComponent>(iconEntity);
                    iconComponent.Color = config.DefaultColor;
                    iconComponent.Scale = config.Scale;
                    iconComponent.IsVisible = config.IsVisible;
                    em.SetComponentData(iconEntity, iconComponent);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Failed to update map icon visual: {ex.Message}");
            }
        }

        private static string GetIconTypeForPlayer(UserData player, float3 position)
        {
            // Determine icon type based on player location and state
            
            // Check if player is in PvP arena
            if (IsPlayerInPvPArena(position))
            {
                return "player_pvp";
            }
            
            // Check if player is in any arena
            if (IsPlayerInArena(position))
            {
                return "player_arena";
            }
            
            // Default player icon
            return "player_default";
        }

        private static bool IsPlayerInArena(float3 position)
        {
            // Check if position is within any configured arena
            // This would check against arena zone configurations
            return false; // Placeholder
        }

        private static bool IsPlayerInPvPArena(float3 position)
        {
            // Check if position is within PvP arena specifically
            return false; // Placeholder
        }

        private static MapIconPrefabConfig GetIconConfig(string iconType)
        {
            return _iconPrefabs.TryGetValue(iconType, out var config) ? config : _iconPrefabs["player_default"];
        }

        private static List<UserData> GetAllOnlinePlayers()
        {
            var result = new List<UserData>();
            
            try
            {
                var em = VAutoCore.EntityManager;
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user))
                    {
                        var characterEntity = user.LocalCharacter.Equals(default) ? Entity.Null : user.LocalCharacter.GetEntityOnServer();
                        result.Add(new UserData
                        {
                            UserEntity = userEntity,
                            CharacterEntity = characterEntity,
                            PlatformId = user.PlatformId,
                            CharacterName = user.CharacterName.ToString(),
                            IsOnline = characterEntity != Entity.Null && em.Exists(characterEntity)
                        });
                    }
                }
                
                userEntities.Dispose();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GlobalMapIconService] Error getting online players: {ex.Message}");
            }
            
            return result;
        }

        private static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Translation>(entity))
                {
                    return em.GetComponentData<Translation>(entity).Value;
                }
                return float3.zero;
            }
            catch
            {
                return float3.zero;
            }
        }

        private static quaternion GetEntityRotation(Entity entity)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                if (em.HasComponent<Rotation>(entity))
                {
                    return em.GetComponentData<Rotation>(entity).Value;
                }
                return quaternion.identity;
            }
            catch
            {
                return quaternion.identity;
            }
        }
        #endregion

        #region Query Methods
        public static int GetActiveIconCount()
        {
            lock (_lock)
            {
                return _playerIcons.Count;
            }
        }

        public static List<MapIconData> GetAllActiveIcons()
        {
            lock (_lock)
            {
                return _playerIcons.Values.ToList();
            }
        }

        public static MapIconData GetPlayerIcon(ulong platformId)
        {
            lock (_lock)
            {
                return _playerIcons.TryGetValue(platformId, out var icon) ? icon : null;
            }
        }

        public static void SetUpdateInterval(float seconds)
        {
            lock (_lock)
            {
                _updateInterval = math.max(1.0f, seconds); // Minimum 1 second
                
                if (_updateTimer != null)
                {
                    _updateTimer.Interval = _updateInterval * 1000;
                }
                
                Log?.LogInfo($"[GlobalMapIconService] Update interval set to {_updateInterval} seconds");
            }
        }

        public static float GetUpdateInterval()
        {
            lock (_lock)
            {
                return _updateInterval;
            }
        }
        #endregion

        #region Data Structures
        public class MapIconData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public Entity IconEntity { get; set; }
            public float3 Position { get; set; }
            public quaternion Rotation { get; set; }
            public string IconType { get; set; }
            public float4 Color { get; set; }
            public float Scale { get; set; }
            public bool IsVisible { get; set; }
            public DateTime LastUpdate { get; set; }
        }

        public class MapIconPrefabConfig
        {
            public string PrefabName { get; set; }
            public string IconType { get; set; }
            public float4 DefaultColor { get; set; }
            public float Scale { get; set; }
            public bool IsVisible { get; set; }
        }

        // Component structs for map icons
        public struct MapIconComponent : IComponentData
        {
            public string IconType;
            public float4 Color;
            public float Scale;
            public bool IsVisible;
        }

        public struct PrefabReferenceComponent : IComponentData
        {
            public string PrefabName;
        }

        public class UserData
        {
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public bool IsOnline { get; set; }
        }
        #endregion
    }
}