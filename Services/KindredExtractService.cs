using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Gameplay;
using ProjectM.Network;
using Stunlock.Core;
using VAuto.Core;
using VAuto.Services;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// KindredExtract Integration Service
    /// Handles extraction and integration of map icons, armors, tiles, and glows
    /// Integrates with zone system and player tracker
    /// </summary>
    public static class KindredExtractService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, ExtractedAssetData> _extractedAssets = new();
        private static readonly Dictionary<ulong, PlayerExtractData> _playerData = new();
        private static ManualLogSource Log => Plugin.Logger;

        // Asset categories
        public enum AssetCategory
        {
            MapIcon,
            Armor,
            Tile,
            Glow,
            Buff,
            Zone
        }

        public struct ExtractedAssetData
        {
            public string Name;
            public PrefabGUID Guid;
            public AssetCategory Category;
            public string Description;
            public float3 Position;
            public Dictionary<string, object> Properties;
        }

        public struct PlayerExtractData
        {
            public ulong PlatformId;
            public string PlayerName;
            public Entity UserEntity;
            public Entity CharacterEntity;
            public float3 LastPosition;
            public DateTime LastExtractTime;
            public Dictionary<AssetCategory, List<PrefabGUID>> DiscoveredAssets;
        }

        #region Initialization
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                Log?.LogInfo("[KindredExtractService] Initializing KindredExtract integration service...");

                _extractedAssets.Clear();
                _playerData.Clear();

                // Load existing extracted data if available
                LoadExtractedData();

                _initialized = true;
                Log?.LogInfo("[KindredExtractService] KindredExtract integration service initialized successfully");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Failed to initialize: {ex.Message}");
            }
        }

        private static void LoadExtractedData()
        {
            try
            {
                string dataPath = Path.Combine(Plugin.ConfigPath, "KindredExtract");
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                    return;
                }

                // Load extracted prefabs
                string prefabsFile = Path.Combine(dataPath, "extracted_prefabs.json");
                if (File.Exists(prefabsFile))
                {
                    // TODO: Implement JSON loading for extracted prefabs
                    Log?.LogInfo("[KindredExtractService] Loaded extracted prefabs data");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Failed to load extracted data: {ex.Message}");
            }
        }
        #endregion

        #region Player Events
        public static void OnPlayerConnected(Entity userEntity, string playerName)
        {
            try
            {
                if (!VRCore.EM.TryGetComponentData(userEntity, out User user))
                    return;

                ulong platformId = user.PlatformId;
                Entity characterEntity = user.LocalCharacter._Entity;

                var playerData = new PlayerExtractData
                {
                    PlatformId = platformId,
                    PlayerName = playerName,
                    UserEntity = userEntity,
                    CharacterEntity = characterEntity,
                    LastPosition = float3.zero,
                    LastExtractTime = DateTime.UtcNow,
                    DiscoveredAssets = new Dictionary<AssetCategory, List<PrefabGUID>>()
                };

                _playerData[platformId] = playerData;

                // Extract nearby assets for new player
                ExtractNearbyAssets(characterEntity, 50f);

                Log?.LogInfo($"[KindredExtractService] Player {playerName} connected and initialized for extraction");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in OnPlayerConnected: {ex.Message}");
            }
        }

        public static void OnPlayerDisconnected(Entity userEntity, string playerName)
        {
            try
            {
                if (!VRCore.EM.TryGetComponentData(userEntity, out User user))
                    return;

                ulong platformId = user.PlatformId;
                if (_playerData.ContainsKey(platformId))
                {
                    // Save player's discovered assets
                    SavePlayerExtractData(platformId);
                    _playerData.Remove(platformId);
                }

                Log?.LogInfo($"[KindredExtractService] Player {playerName} disconnected and data saved");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in OnPlayerDisconnected: {ex.Message}");
            }
        }

        public static void OnPlayerCreated(Entity userEntity, string playerName)
        {
            try
            {
                Log?.LogInfo($"[KindredExtractService] New player {playerName} created - initializing extraction systems");
                
                // Initialize extraction systems for new player
                OnPlayerConnected(userEntity, playerName);
                
                // Extract initial zone data
                ExtractZoneData();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in OnPlayerCreated: {ex.Message}");
            }
        }
        #endregion

        #region Asset Extraction
        public static void ExtractNearbyAssets(Entity characterEntity, float radius)
        {
            try
            {
                if (!VRCore.EM.TryGetComponentData(characterEntity, out Translation translation))
                    return;

                float3 playerPos = translation.Value;

                // Extract nearby prefabs
                ExtractNearbyPrefabs(playerPos, radius);

                // Extract nearby tile models
                ExtractNearbyTileModels(playerPos, radius);

                // Extract nearby buffs
                ExtractNearbyBuffs(playerPos, radius);

                // Extract zone data
                ExtractZoneDataForPosition(playerPos);

                Log?.LogInfo($"[KindredExtractService] Extracted assets within {radius}m of player position");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractNearbyAssets: {ex.Message}");
            }
        }

        private static void ExtractNearbyPrefabs(float3 position, float radius)
        {
            try
            {
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>(), 
                                                       ComponentType.ReadOnly<Translation>());
                var entities = query.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                {
                    if (!VRCore.EM.TryGetComponentData(entity, out Translation translation))
                        continue;

                    float distance = math.distance(position, translation.Value);
                    if (distance <= radius)
                    {
                        if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID guid))
                        {
                            var assetData = new ExtractedAssetData
                            {
                                Name = $"Prefab_{guid.GuidHash}",
                                Guid = guid,
                                Category = DetermineAssetCategory(entity, guid),
                                Position = translation.Value,
                                Properties = new Dictionary<string, object>()
                            };

                            _extractedAssets[assetData.Name] = assetData;
                        }
                    }
                }

                entities.Dispose();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractNearbyPrefabs: {ex.Message}");
            }
        }

        private static void ExtractNearbyTileModels(float3 position, float radius)
        {
            try
            {
                // Query for tile model entities
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>(),
                                                       ComponentType.ReadOnly<Translation>(),
                                                       ComponentType.ReadOnly<TileModel>());
                var entities = query.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                {
                    if (!VRCore.EM.TryGetComponentData(entity, out Translation translation))
                        continue;

                    float distance = math.distance(position, translation.Value);
                    if (distance <= radius)
                    {
                        if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID guid))
                        {
                            var assetData = new ExtractedAssetData
                            {
                                Name = $"Tile_{guid.GuidHash}",
                                Guid = guid,
                                Category = AssetCategory.Tile,
                                Position = translation.Value,
                                Properties = new Dictionary<string, object>()
                            };

                            if (VRCore.EM.TryGetComponentData(entity, out TileModel tileModel))
                            {
                                assetData.Properties["TileType"] = tileModel.GetType().Name;
                            }

                            _extractedAssets[assetData.Name] = assetData;
                        }
                    }
                }

                entities.Dispose();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractNearbyTileModels: {ex.Message}");
            }
        }

        private static void ExtractNearbyBuffs(float3 position, float radius)
        {
            try
            {
                // Query for buff entities
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>(),
                                                       ComponentType.ReadOnly<Translation>(),
                                                       ComponentType.ReadOnly<Buff>());
                var entities = query.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                {
                    if (!VRCore.EM.TryGetComponentData(entity, out Translation translation))
                        continue;

                    float distance = math.distance(position, translation.Value);
                    if (distance <= radius)
                    {
                        if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID guid))
                        {
                            var assetData = new ExtractedAssetData
                            {
                                Name = $"Buff_{guid.GuidHash}",
                                Guid = guid,
                                Category = AssetCategory.Buff,
                                Position = translation.Value,
                                Properties = new Dictionary<string, object>()
                            };

                            _extractedAssets[assetData.Name] = assetData;
                        }
                    }
                }

                entities.Dispose();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractNearbyBuffs: {ex.Message}");
            }
        }

        private static void ExtractZoneData()
        {
            try
            {
                // Extract map zones
                ExtractMapZones();

                // Extract world region polygons
                ExtractWorldRegionPolygons();

                // Extract spawn regions
                ExtractSpawnRegions();

                Log?.LogInfo("[KindredExtractService] Zone data extraction completed");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractZoneData: {ex.Message}");
            }
        }

        private static void ExtractZoneDataForPosition(float3 position)
        {
            try
            {
                // Extract zones relevant to player position
                // This would integrate with the existing zone system
                if (Core.ZoneService != null)
                {
                    // Use existing zone service to get zone info
                    var zoneInfo = Core.ZoneService.GetZoneAtPosition(position);
                    if (zoneInfo != null)
                    {
                        var assetData = new ExtractedAssetData
                        {
                            Name = $"Zone_{zoneInfo.Name}",
                            Guid = new PrefabGUID(), // Zone might not have a GUID
                            Category = AssetCategory.Zone,
                            Position = position,
                            Properties = new Dictionary<string, object>
                            {
                                ["ZoneName"] = zoneInfo.Name,
                                ["ZoneType"] = zoneInfo.Type.ToString()
                            }
                        };

                        _extractedAssets[assetData.Name] = assetData;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error in ExtractZoneDataForPosition: {ex.Message}");
            }
        }

        private static void ExtractMapZones()
        {
            // Implementation for extracting map zones
            Log?.LogInfo("[KindredExtractService] Extracting map zones...");
        }

        private static void ExtractWorldRegionPolygons()
        {
            // Implementation for extracting world region polygons
            Log?.LogInfo("[KindredExtractService] Extracting world region polygons...");
        }

        private static void ExtractSpawnRegions()
        {
            // Implementation for extracting spawn regions
            Log?.LogInfo("[KindredExtractService] Extracting spawn regions...");
        }
        #endregion

        #region Utility Methods
        private static AssetCategory DetermineAssetCategory(Entity entity, PrefabGUID guid)
        {
            try
            {
                // Check for armor items
                if (VRCore.EM.HasComponent<Equipment>(entity))
                    return AssetCategory.Armor;

                // Check for map icons
                if (entity.ToString().Contains("MapIcon") || VRCore.EM.HasComponent<MapIcon>(entity))
                    return AssetCategory.MapIcon;

                // Check for glow effects
                if (VRCore.EM.HasComponent<GlowEffect>(entity) || VRCore.EM.HasComponent<Buff>(entity))
                    return AssetCategory.Glow;

                // Default to tile for building prefabs
                if (VRCore.EM.HasComponent<Building>(entity))
                    return AssetCategory.Tile;

                return AssetCategory.Tile; // Default category
            }
            catch
            {
                return AssetCategory.Tile;
            }
        }

        private static void SavePlayerExtractData(ulong platformId)
        {
            try
            {
                if (!_playerData.ContainsKey(platformId))
                    return;

                var playerData = _playerData[platformId];
                string dataPath = Path.Combine(Plugin.ConfigPath, "KindredExtract", "Players");
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                string playerFile = Path.Combine(dataPath, $"{platformId}_extracts.json");
                // TODO: Implement JSON serialization for player data

                Log?.LogInfo($"[KindredExtractService] Saved extract data for player {playerData.PlayerName}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error saving player extract data: {ex.Message}");
            }
        }

        public static List<ExtractedAssetData> GetAssetsByCategory(AssetCategory category)
        {
            return _extractedAssets.Values.Where(a => a.Category == category).ToList();
        }

        public static ExtractedAssetData? GetAssetByName(string name)
        {
            return _extractedAssets.TryGetValue(name, out var asset) ? asset : null;
        }

        public static Dictionary<AssetCategory, int> GetAssetCounts()
        {
            return _extractedAssets.Values
                .GroupBy(a => a.Category)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        #endregion

        #region Integration with Existing Systems
        /// <summary>
        /// Integration with GlobalMapIconService
        /// </summary>
        public static void UpdateMapIcons()
        {
            try
            {
                var mapIcons = GetAssetsByCategory(AssetCategory.MapIcon);
                foreach (var icon in mapIcons)
                {
                    // Update global map icon system with extracted icons
                    // GlobalMapIconService.AddCustomIcon(icon.Name, icon.Guid, icon.Position);
                    Plugin.Logger?.LogInfo($"[KindredExtractService] Would add map icon: {icon.Name}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error updating map icons: {ex.Message}");
            }
        }

        /// <summary>
        /// Integration with ArenaGlowService
        /// </summary>
        public static void UpdateGlowEffects()
        {
            try
            {
                var glows = GetAssetsByCategory(AssetCategory.Glow);
                foreach (var glow in glows)
                {
                    // Update arena glow system with extracted effects
                    // ArenaGlowService.AddCustomGlow(glow.Name, glow.Guid, glow.Position);
                    Plugin.Logger?.LogInfo($"[KindredExtractService] Would add glow effect: {glow.Name}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error updating glow effects: {ex.Message}");
            }
        }

        /// <summary>
        /// Integration with Tile system
        /// </summary>
        public static void UpdateTileSystem()
        {
            try
            {
                var tiles = GetAssetsByCategory(AssetCategory.Tile);
                foreach (var tile in tiles)
                {
                    // Update tile system with extracted tiles
                    Data.Tile.Named[tile.Name] = tile.Guid;
                    Plugin.Logger?.LogInfo($"[KindredExtractService] Added tile: {tile.Name}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractService] Error updating tile system: {ex.Message}");
            }
        }
        #endregion
    }
}
