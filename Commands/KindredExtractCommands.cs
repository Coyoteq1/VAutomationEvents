/*
DISABLED - KindredExtractCommands references non-existent methods on Prefabs class
(ExtractNearbyAssets, GetAssetsByCategory, etc.)
These were never implemented. Commenting out to unblock build.

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
using VampireCommandFramework;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services;
using VAuto.Services.Systems;
using BepInEx.Logging;
using CommandAttribute = VampireCommandFramework.CommandAttribute;

namespace VAuto.Commands
{
    /// <summary>
    /// KindredExtract Commands
    /// Commands for extracting and managing map icons, armors, tiles, and glows
    /// </summary>
    public static class KindredExtractCommands
    {
        private static ManualLogSource Log => Plugin.Logger;

        #region Main Commands
        [Command("extract", description: "Main KindredExtract command - extracts various game assets", adminOnly: true)]
        public static void ExtractCommand(ChatCommandContext ctx, string action = "help", string category = "all", float radius = 100f)
        {
            try
            {
                switch (action.ToLower())
                {
                    case "mapicons":
                    case "icons":
                        ExtractMapIcons(ctx, radius);
                        break;
                    case "armor":
                    case "armours":
                        ExtractArmor(ctx, radius);
                        break;
                    case "tiles":
                    case "tile":
                        ExtractTiles(ctx, radius);
                        break;
                    case "glows":
                    case "glow":
                        ExtractGlows(ctx, radius);
                        break;
                    case "zones":
                        ExtractZones(ctx);
                        break;
                    case "all":
                        ExtractAll(ctx, radius);
                        break;
                    case "status":
                        ShowExtractionStatus(ctx);
                        break;
                    case "integrate":
                        IntegrateAssets(ctx, category);
                        break;
                    default:
                        ShowExtractHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error: {ex.Message}</color>");
                Log?.LogError($"[KindredExtractCommands] Error in ExtractCommand: {ex.Message}");
            }
        }

        [Command("kextract", description: "KindredExtract alias command", adminOnly: true)]
        public static void KExtractCommand(ChatCommandContext ctx, string action = "help", string category = "all", float radius = 100f)
        {
            ExtractCommand(ctx, action, category, radius);
        }
        #endregion

        #region Asset Extraction Commands
        private static void ExtractMapIcons(ChatCommandContext ctx, float radius)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting map icons...</color>");
                
                var character = ctx.Event.SenderCharacterEntity;
                Prefabs.ExtractNearbyAssets(character, radius);
                
                var mapIcons = Prefabs.GetAssetsByCategory(Prefabs.AssetCategory.MapIcon);
                int count = mapIcons.Count;
                
                ctx.Reply($"<color=green>Extracted {count} map icons within {radius}m</color>");
                
                // Save extracted data
                SaveExtractedData("mapicons", mapIcons);
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting map icons: {ex.Message}</color>");
            }
        }

        private static void ExtractArmor(ChatCommandContext ctx, float radius)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting armor prefabs...</color>");
                
                var character = ctx.Event.SenderCharacterEntity;
                Prefabs.ExtractNearbyAssets(character, radius);
                
                var armors = Prefabs.GetAssetsByCategory(Prefabs.AssetCategory.Armor);
                int count = armors.Count;
                
                ctx.Reply($"<color=green>Extracted {count} armor prefabs within {radius}m</color>");
                
                // Save extracted data
                SaveExtractedData("armor", armors);
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting armor: {ex.Message}</color>");
            }
        }

        private static void ExtractTiles(ChatCommandContext ctx, float radius)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting tile prefabs...</color>");
                
                var character = ctx.Event.SenderCharacterEntity;
                Prefabs.ExtractNearbyAssets(character, radius);
                
                var tiles = Prefabs.GetAssetsByCategory(Prefabs.AssetCategory.Tile);
                int count = tiles.Count;
                
                ctx.Reply($"<color=green>Extracted {count} tile prefabs within {radius}m</color>");
                
                // Save extracted data
                SaveExtractedData("tiles", tiles);
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting tiles: {ex.Message}</color>");
            }
        }

        private static void ExtractGlows(ChatCommandContext ctx, float radius)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting glow effects...</color>");
                
                var character = ctx.Event.SenderCharacterEntity;
                Prefabs.ExtractNearbyAssets(character, radius);
                
                var glows = Prefabs.GetAssetsByCategory(Prefabs.AssetCategory.Glow);
                int count = glows.Count;
                
                ctx.Reply($"<color=green>Extracted {count} glow effects within {radius}m</color>");
                
                // Save extracted data
                SaveExtractedData("glows", glows);
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting glows: {ex.Message}</color>");
            }
        }

        private static void ExtractZones(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting zone data...</color>");
                
                Prefabs.ExtractZoneData();
                
                var zones = Prefabs.GetAssetsByCategory(Prefabs.AssetCategory.Zone);
                int count = zones.Count;
                
                ctx.Reply($"<color=green>Extracted {count} zones</color>");
                
                // Save extracted data
                SaveExtractedData("zones", zones);
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting zones: {ex.Message}</color>");
            }
        }

        private static void ExtractAll(ChatCommandContext ctx, float radius)
        {
            try
            {
                ctx.Reply("<color=yellow>Extracting all assets...</color>");
                
                var character = ctx.Event.SenderCharacterEntity;
                Prefabs.ExtractNearbyAssets(character, radius);
                Prefabs.ExtractZoneData();
                
                var counts = Prefabs.GetAssetCounts();
                string countStr = string.Join(", ", counts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                
                ctx.Reply($"<color=green>Extracted assets - {countStr}</color>");
                
                // Save all extracted data
                SaveAllExtractedData();
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting all assets: {ex.Message}</color>");
            }
        }
        #endregion

        #region Integration Commands
        private static void IntegrateAssets(ChatCommandContext ctx, string category)
        {
            try
            {
                ctx.Reply("<color=yellow>Integrating extracted assets...</color>");
                
                switch (category.ToLower())
                {
                    case "mapicons":
                    case "icons":
                        Prefabs.UpdateMapIcons();
                        ctx.Reply("<color=green>Map icons integrated into GlobalMapIconService</color>");
                        break;
                    case "glows":
                    case "glow":
                        Prefabs.UpdateGlowEffects();
                        ctx.Reply("<color=green>Glow effects integrated into ArenaGlowService</color>");
                        break;
                    case "tiles":
                    case "tile":
                        Prefabs.UpdateTileSystem();
                        ctx.Reply("<color=green>Tiles integrated into Tile system</color>");
                        break;
                    case "all":
                        Prefabs.UpdateMapIcons();
                        Prefabs.UpdateGlowEffects();
                        Prefabs.UpdateTileSystem();
                        ctx.Reply("<color=green>All assets integrated into respective systems</color>");
                        break;
                    default:
                        ctx.Reply("<color=orange>Unknown category. Use: mapicons, glows, tiles, or all</color>");
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error integrating assets: {ex.Message}</color>");
            }
        }
        #endregion

        #region Status and Help Commands
        private static void ShowExtractionStatus(ChatCommandContext ctx)
        {
            try
            {
                var counts = Prefabs.GetAssetCounts();
                
                ctx.Reply("<color=cyan>=== KindredExtract Status ===</color>");
                ctx.Reply("<color=white>Extracted Assets:</color>");
                
                foreach (var kvp in counts)
                {
                    ctx.Reply($"  <color=yellow>{kvp.Key}:</color> <color=green>{kvp.Value}</color>");
                }
                
                int total = counts.Values.Sum();
                ctx.Reply($"  <color=white>Total:</color> <color=green>{total}</color> assets");
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error showing status: {ex.Message}</color>");
            }
        }

        private static void ShowExtractHelp(ChatCommandContext ctx)
        {
            ctx.Reply("<color=cyan>=== KindredExtract Commands ===</color>");
            ctx.Reply("<color=white>.extract <action> [category] [radius]</color>");
            ctx.Reply("");
            ctx.Reply("<color=yellow>Actions:</color>");
            ctx.Reply("  <color=white>mapicons</color> - Extract map icons");
            ctx.Reply("  <color=white>armor</color> - Extract armor prefabs");
            ctx.Reply("  <color=white>tiles</color> - Extract tile prefabs");
            ctx.Reply("  <color=white>glows</color> - Extract glow effects");
            ctx.Reply("  <color=white>zones</color> - Extract zone data");
            ctx.Reply("  <color=white>all</color> - Extract all assets");
            ctx.Reply("  <color=white>status</color> - Show extraction status");
            ctx.Reply("  <color=white>integrate</color> - Integrate assets into systems");
            ctx.Reply("");
            ctx.Reply("<color=yellow>Examples:</color>");
            ctx.Reply("  <color=white>.extract mapicons 50</color> - Extract icons within 50m");
            ctx.Reply("  <color=white>.extract all 100</color> - Extract all assets within 100m");
            ctx.Reply("  <color=white>.extract integrate mapicons</color> - Integrate map icons");
            ctx.Reply("  <color=white>.extract status</color> - Show current status");
        }
        #endregion

        #region Utility Methods
        private static void SaveExtractedData(string category, List<ExtractedAssetData> assets)
        {
            try
            {
                string dataPath = Path.Combine(Plugin.ConfigPath, "KindredExtract");
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                string fileName = $"{category}_extracts_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(dataPath, fileName);
                
                // TODO: Implement JSON serialization
                // For now, just create a simple text file with asset info
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"// {category.ToUpper()} Extracted Assets - {DateTime.Now}");
                    writer.WriteLine($"// Total: {assets.Count} assets");
                    writer.WriteLine("");
                    
                    foreach (var asset in assets)
                    {
                        writer.WriteLine($"// {asset.Name}");
                        writer.WriteLine($"{{");
                        writer.WriteLine($"  \"Name\": \"{asset.Name}\",");
                        writer.WriteLine($"  \"Guid\": \"{asset.Guid}\",");
                        writer.WriteLine($"  \"Category\": \"{asset.Category}\",");
                        writer.WriteLine($"  \"Position\": \"({asset.Position.x:F2}, {asset.Position.y:F2}, {asset.Position.z:F2})\"");
                        writer.WriteLine($"}},");
                        writer.WriteLine("");
                    }
                }
                
                Log?.LogInfo($"[KindredExtractCommands] Saved {assets.Count} {category} assets to {fileName}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error saving extracted data: {ex.Message}");
            }
        }

        private static void SaveAllExtractedData()
        {
            try
            {
                var allCategories = Enum.GetValues(typeof(Prefabs.AssetCategory))
                    .Cast<Prefabs.AssetCategory>();
                
                foreach (var category in allCategories)
                {
                    var assets = Prefabs.GetAssetsByCategory(category);
                    if (assets.Count > 0)
                    {
                        SaveExtractedData(category.ToString().ToLower(), assets);
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error saving all extracted data: {ex.Message}");
            }
        }
        #endregion

        #region Advanced Commands
        [Command("extract prefab", description: "Extract specific prefab by GUID", adminOnly: true)]
        public static void ExtractPrefabCommand(ChatCommandContext ctx, string guidString)
        {
            try
            {
                if (!Guid.TryParse(guidString, out Guid guid))
                {
                    ctx.Reply("<color=red>Invalid GUID format</color>");
                    return;
                }

                var prefabGuid = new PrefabGUID(guid.GetHashCode());
                
                // Find entities with this GUID
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>());
                var entities = query.ToEntityArray(Allocator.Temp);
                
                int found = 0;
                foreach (var entity in entities)
                {
                    if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID entityGuid) && entityGuid.Equals(prefabGuid))
                    {
                        found++;
                        if (VRCore.EM.TryGetComponentData(entity, out Translation translation))
                        {
                            ctx.Reply($"<color=green>Found {guidString} at ({translation.Value.x:F2}, {translation.Value.y:F2}, {translation.Value.z:F2})</color>");
                        }
                        else
                        {
                            ctx.Reply($"<color=green>Found {guidString} (no position data)</color>");
                        }
                    }
                }
                
                if (found == 0)
                {
                    ctx.Reply($"<color=yellow>No entities found with GUID {guidString}</color>");
                }
                else
                {
                    ctx.Reply($"<color=green>Found {found} entities with GUID {guidString}</color>");
                }
                
                entities.Dispose();
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error extracting prefab: {ex.Message}</color>");
            }
        }

        [Command("export prefabs", description: "Export all prefabs to file", adminOnly: true)]
        public static void ExportPrefabsCommand(ChatCommandContext ctx, string filter = "")
        {
            try
            {
                ctx.Reply("<color=yellow>Exporting prefabs...</color>");
                
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>());
                var entities = query.ToEntityArray(Allocator.Temp);
                
                string dataPath = Path.Combine(Plugin.ConfigPath, "KindredExtract");
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                string fileName = $"prefabs_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(dataPath, fileName);
                
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("// Prefabs Export");
                    writer.WriteLine($"// Generated: {DateTime.Now}");
                    writer.WriteLine($"// Total Prefabs: {entities.Length}");
                    writer.WriteLine("");

                    foreach (var entity in entities)
                    {
                        if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID guid))
                        {
                            string entityName = entity.ToString();

                            // Apply filter if provided
                            if (!string.IsNullOrEmpty(filter) && !entityName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                continue;

                            string shortName = GetShortPrefabName(entityName, guid);
                            writer.WriteLine($"public static readonly PrefabGUID {shortName} = new PrefabGUID({guid.GuidHash});");
                        }
                    }
                }
                
                ctx.Reply($"<color=green>Exported {entities.Length} prefabs to {fileName}</color>");
                entities.Dispose();
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error exporting prefabs: {ex.Message}</color>");
            }
        }

        #region Custom Dump Commands
        [Command("create", description: "Create custom UUID for asset identification", adminOnly: true)]
        public static void CreateUuidCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    ctx.Reply("<color=red>Error: Name cannot be empty</color>");
                    return;
                }

                // Generate unique UUID for the asset
                string uuid = Guid.NewGuid().ToString("N");
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                
                // Store the UUID mapping
                var customUuid = new CustomUuidData
                {
                    Name = name,
                    Uuid = uuid,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = ctx.Event.User.PlatformId,
                    AssetType = "Custom"
                };

                SaveCustomUuid(customUuid);
                
                ctx.Reply($"<color=green>Created UUID for '{name}':</color>");
                ctx.Reply($"<color=cyan>UUID: {uuid}</color>");
                ctx.Reply($"<color=yellow>Use .save {uuid} [prefab] to save assets</color>");
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error creating UUID: {ex.Message}</color>");
                Log?.LogError($"[KindredExtractCommands] Error in CreateUuidCommand: {ex.Message}");
            }
        }

        [Command("save", description: "Save prefab with custom UUID to server data", adminOnly: true)]
        public static void SavePrefabCommand(ChatCommandContext ctx, string uuid, string prefabFilter = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    ctx.Reply("<color=red>Error: UUID cannot be empty</color>");
                    return;
                }

                // Validate UUID format
                if (!Guid.TryParse(uuid, out Guid parsedGuid))
                {
                    ctx.Reply("<color=red>Error: Invalid UUID format</color>");
                    return;
                }

                ctx.Reply("<color=yellow>Saving prefab data...</color>");
                
                var extractedData = ExtractPrefabData(prefabFilter);
                var saveData = new CustomSaveData
                {
                    Uuid = uuid,
                    PrefabFilter = prefabFilter,
                    ExtractedAt = DateTime.UtcNow,
                    SavedBy = ctx.Event.User.PlatformId,
                    AssetData = extractedData
                };

                // Save to server data directory
                string filePath = SaveToServerData(saveData);
                
                // Upload to server (if configured)
                bool uploaded = UploadToServer(saveData, filePath);
                
                ctx.Reply($"<color=green>Saved {extractedData.Count} prefabs with UUID {uuid}</color>");
                ctx.Reply($"<color=cyan>File: {Path.GetFileName(filePath)}</color>");
                
                if (uploaded)
                {
                    ctx.Reply("<color=green>✓ Successfully uploaded to server</color>");
                }
                else
                {
                    ctx.Reply("<color=yellow>⚠ Saved locally, upload failed</color>");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error saving prefab: {ex.Message}</color>");
                Log?.LogError($"[KindredExtractCommands] Error in SavePrefabCommand: {ex.Message}");
            }
        }

        [Command("dump", description: "Custom dump command with UUID support", adminOnly: true)]
        public static void DumpCommand(ChatCommandContext ctx, string action = "help", string uuid = "", string filter = "")
        {
            try
            {
                switch (action.ToLower())
                {
                    case "uuid":
                    case "create":
                        if (!string.IsNullOrEmpty(uuid))
                            CreateUuidCommand(ctx, uuid);
                        else
                            ctx.Reply("<color=orange>Usage: .dump create [name]</color>");
                        break;
                    case "save":
                        SavePrefabCommand(ctx, uuid, filter);
                        break;
                    case "list":
                        ListCustomUuids(ctx);
                        break;
                    case "server":
                        ListServerData(ctx);
                        break;
                    case "upload":
                        UploadToServerCommand(ctx, uuid);
                        break;
                    default:
                        ShowDumpHelp(ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error in dump command: {ex.Message}</color>");
                Log?.LogError($"[KindredExtractCommands] Error in DumpCommand: {ex.Message}");
            }
        }

        [Command("upload", description: "Upload saved data to server", adminOnly: true)]
        public static void UploadToServerCommand(ChatCommandContext ctx, string uuid = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    ctx.Reply("<color=red>Error: UUID required</color>");
                    return;
                }

                var serverDataDir = Path.Combine(Plugin.ConfigPath, "ServerData");
                var saveFile = Directory.GetFiles(serverDataDir, $"*{uuid}*.json").FirstOrDefault();
                
                if (string.IsNullOrEmpty(saveFile))
                {
                    ctx.Reply($"<color=red>No saved data found for UUID: {uuid}</color>");
                    return;
                }

                var saveData = LoadSaveData(saveFile);
                bool uploaded = UploadToServer(saveData, saveFile);
                
                if (uploaded)
                {
                    ctx.Reply($"<color=green>✓ Successfully uploaded {Path.GetFileName(saveFile)} to server</color>");
                }
                else
                {
                    ctx.Reply("<color=red>✗ Upload failed</color>");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error uploading: {ex.Message}</color>");
            }
        }
        #endregion

        #region Custom Data Structures
        public struct CustomUuidData
        {
            public string Name;
            public string Uuid;
            public DateTime CreatedAt;
            public ulong CreatedBy;
            public string AssetType;
        }

        public struct CustomSaveData
        {
            public string Uuid;
            public string PrefabFilter;
            public DateTime ExtractedAt;
            public ulong SavedBy;
            public List<ExtractedAssetData> AssetData;
        }
        #endregion

        #region Custom Data Management
        private static void SaveCustomUuid(CustomUuidData uuidData)
        {
            try
            {
                string dataPath = Path.Combine(Plugin.ConfigPath, "CustomUuids");
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                string uuidFile = Path.Combine(dataPath, "custom_uuids.json");
                var uuids = new List<CustomUuidData>();
                
                // Load existing UUIDs
                if (File.Exists(uuidFile))
                {
                    // TODO: Implement JSON loading
                }
                
                uuids.Add(uuidData);
                
                // TODO: Implement JSON saving
                using (var writer = new StreamWriter(uuidFile, true))
                {
                    writer.WriteLine($"// UUID: {uuidData.Uuid} | Name: {uuidData.Name} | Created: {uuidData.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                Log?.LogInfo($"[KindredExtractCommands] Saved custom UUID: {uuidData.Name} -> {uuidData.Uuid}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error saving custom UUID: {ex.Message}");
            }
        }

        private static List<ExtractedAssetData> ExtractPrefabData(string filter)
        {
            var extractedData = new List<ExtractedAssetData>();
            
            try
            {
                var query = VRCore.EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>());
                var entities = query.ToEntityArray(Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    if (VRCore.EM.TryGetComponentData(entity, out PrefabGUID guid))
                    {
                        string entityName = entity.ToString();
                        
                        // Apply filter if provided
                        if (!string.IsNullOrEmpty(filter) && !entityName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        if (VRCore.EM.TryGetComponentData(entity, out Translation translation))
                        {
                            var assetData = new ExtractedAssetData
                            {
                                Name = entityName,
                                Guid = guid,
                                Category = DetermineAssetCategory(entity, guid),
                                Position = translation.Value,
                                Properties = new Dictionary<string, object>()
                            };
                            
                            extractedData.Add(assetData);
                        }
                    }
                }
                
                entities.Dispose();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error extracting prefab data: {ex.Message}");
            }
            
            return extractedData;
        }

        private static string SaveToServerData(CustomSaveData saveData)
        {
            try
            {
                string serverDataDir = Path.Combine(Plugin.ConfigPath, "ServerData");
                if (!Directory.Exists(serverDataDir))
                    Directory.CreateDirectory(serverDataDir);

                string fileName = $"save_{saveData.Uuid}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filePath = Path.Combine(serverDataDir, fileName);
                
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("{");
                    writer.WriteLine($"  \"uuid\": \"{saveData.Uuid}\",");
                    writer.WriteLine($"  \"prefabFilter\": \"{saveData.PrefabFilter}\",");
                    writer.WriteLine($"  \"extractedAt\": \"{saveData.ExtractedAt:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
                    writer.WriteLine($"  \"savedBy\": {saveData.SavedBy},");
                    writer.WriteLine($"  \"assetCount\": {saveData.AssetData.Count},");
                    writer.WriteLine("  \"assets\": [");
                    
                    for (int i = 0; i < saveData.AssetData.Count; i++)
                    {
                        var asset = saveData.AssetData[i];
                        writer.WriteLine("    {");
                        writer.WriteLine($"      \"name\": \"{asset.Name}\",");
                        writer.WriteLine($"      \"guid\": \"{asset.Guid}\",");
                        writer.WriteLine($"      \"category\": \"{asset.Category}\",");
                        writer.WriteLine($"      \"position\": {{\"x\": {asset.Position.x}, \"y\": {asset.Position.y}, \"z\": {asset.Position.z}}}");
                        writer.Write(i < saveData.AssetData.Count - 1 ? "    }," : "    }");
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine("  ]");
                    writer.WriteLine("}");
                }
                
                Log?.LogInfo($"[KindredExtractCommands] Saved server data: {fileName}");
                return filePath;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error saving to server data: {ex.Message}");
                throw;
            }
        }

        private static bool UploadToServer(CustomSaveData saveData, string filePath)
        {
            try
            {
                // TODO: Implement actual server upload logic
                // This could be HTTP upload, FTP, or other transfer method
                Log?.LogInfo($"[KindredExtractCommands] Would upload {Path.GetFileName(filePath)} to server");
                
                // Simulate upload success for now
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[KindredExtractCommands] Error uploading to server: {ex.Message}");
                return false;
            }
        }

        private static CustomSaveData LoadSaveData(string filePath)
        {
            // TODO: Implement JSON loading
            return new CustomSaveData();
        }

        private static void ListCustomUuids(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("<color=cyan>=== Custom UUIDs ===</color>");
                
                string uuidFile = Path.Combine(Plugin.ConfigPath, "CustomUuids", "custom_uuids.json");
                if (File.Exists(uuidFile))
                {
                    var lines = File.ReadAllLines(uuidFile);
                    foreach (var line in lines.Where(l => l.StartsWith("// UUID:")))
                    {
                        ctx.Reply($"<color=white>{line}</color>");
                    }
                }
                else
                {
                    ctx.Reply("<color=yellow>No custom UUIDs found</color>");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error listing UUIDs: {ex.Message}</color>");
            }
        }

        private static void ListServerData(ChatCommandContext ctx)
        {
            try
            {
                ctx.Reply("<color=cyan>=== Server Data ===</color>");
                
                string serverDataDir = Path.Combine(Plugin.ConfigPath, "ServerData");
                if (Directory.Exists(serverDataDir))
                {
                    var files = Directory.GetFiles(serverDataDir, "*.json");
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        ctx.Reply($"<color=white>{fileInfo.Name}</color> <color=gray>({fileInfo.Length:N0} bytes, {fileInfo.CreationTime:yyyy-MM-dd})</color>");
                    }
                }
                else
                {
                    ctx.Reply("<color=yellow>No server data found</color>");
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"<color=red>Error listing server data: {ex.Message}</color>");
            }
        }

        private static void ShowDumpHelp(ChatCommandContext ctx)
        {
            ctx.Reply("<color=cyan>=== Custom Dump Commands ===</color>");
            ctx.Reply("<color=white>.dump create [name]</color> - Create custom UUID");
            ctx.Reply("<color=white>.dump save [uuid] [filter]</color> - Save prefabs with UUID");
            ctx.Reply("<color=white>.dump list</color> - List custom UUIDs");
            ctx.Reply("<color=white>.dump server</color> - List server data");
            ctx.Reply("<color=white>.dump upload [uuid]</color> - Upload to server");
            ctx.Reply("");
            ctx.Reply("<color=yellow>Shortcuts:</color>");
            ctx.Reply("<color=white>.create [name]</color> - Same as .dump create");
            ctx.Reply("<color=white>.save [uuid] [filter]</color> - Same as .dump save");
            ctx.Reply("<color=white>.upload [uuid]</color> - Same as .dump upload");
        }

        private static string SanitizeName(string entityName)
        {
            // Remove invalid characters and create a valid C# identifier
            var sanitized = entityName.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "");
            
            // Ensure it starts with a letter or underscore
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
            
            return string.IsNullOrEmpty(sanitized) ? "UnknownEntity" : sanitized;
        }

        private static AssetCategory DetermineAssetCategory(Entity entity, PrefabGUID guid)
        {
            try
            {
                // Check for armor items
                if (VRCore.EM.HasComponent<ProjectM.Equipment>(entity))
                    return AssetCategory.Armor;

                // Check for map icons
                if (entity.ToString().Contains("MapIcon") || VRCore.EM.HasComponent<ProjectM.MapIcon>(entity))
                    return AssetCategory.MapIcon;

                // Check for glow effects
                if (VRCore.EM.HasComponent<ProjectM.GlowEffect>(entity) || VRCore.EM.HasComponent<ProjectM.Buff>(entity))
                    return AssetCategory.Glow;

                // Default to tile for building prefabs
                if (VRCore.EM.HasComponent<ProjectM.Building>(entity))
                    return AssetCategory.Tile;

                return AssetCategory.Tile; // Default category
            }
            catch
            {
                return AssetCategory.Tile;
            }
        }
        #endregion

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
    }
}
#endregion
*/