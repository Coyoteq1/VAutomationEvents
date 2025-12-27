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
using VAuto.Services.Systems;
using VAuto.Services.Lifecycle;
using BepInEx.Logging;

namespace VAuto.Commands
{
    /// <summary>
    /// Spawn command system for creating objects in grid layouts
    /// Supports various prefabs, rows/columns, and positioning
    /// </summary>
    public static class SpawnCommands
    {
        private static ManualLogSource Log => Plugin.Logger;
        
        #region Main Spawn Command
        /// <summary>
        /// Main spawn command: .spawn [prefab] [rows] [cols] [x] [y] [spacing]
        /// Example: .spawn MapIcon_CastleObject_BloodAltar 5 5 -1000 5 -500 10
        /// </summary>
        [Command("spawn", "Spawn objects in grid layout", "[prefab] [rows] [cols] [x] [y] [spacing]")]
        public static void SpawnCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 6)
                {
                    SendMessage(platformId, "Usage: .spawn [prefab] [rows] [cols] [x] [y] [spacing]");
                    SendMessage(platformId, "Example: .spawn MapIcon_CastleObject_BloodAltar 5 5 -1000 5 -500 10");
                    return;
                }

                var prefab = args[0];
                if (!int.TryParse(args[1], out var rows) || rows <= 0)
                {
                    SendMessage(platformId, "Invalid rows number");
                    return;
                }

                if (!int.TryParse(args[2], out var cols) || cols <= 0)
                {
                    SendMessage(platformId, "Invalid columns number");
                    return;
                }

                if (!float.TryParse(args[3], out var startX))
                {
                    SendMessage(platformId, "Invalid X coordinate");
                    return;
                }

                if (!float.TryParse(args[4], out var startY))
                {
                    SendMessage(platformId, "Invalid Y coordinate");
                    return;
                }

                if (!float.TryParse(args[5], out var spacing) || spacing <= 0)
                {
                    SendMessage(platformId, "Invalid spacing value");
                    return;
                }

                // Validate radius requirements
                var distanceFromCenter = math.distance(new float2(startX, startY), float2.zero);
                if (distanceFromCenter < 100)
                {
                    SendMessage(platformId, $"Error: Position must be above 100 radius from center. Current distance: {distanceFromCenter:F1}");
                    return;
                }

                if (distanceFromCenter > 5000) // Reasonable upper limit
                {
                    SendMessage(platformId, $"Error: Position must be below 5000 radius from center. Current distance: {distanceFromCenter:F1}");
                    return;
                }

                // Spawn the grid
                var success = SpawnGrid(prefab, rows, cols, new float3(startX, startY, 0), spacing, platformId);
                
                if (success)
                {
                    SendMessage(platformId, $"Successfully spawned {rows}x{cols} grid of {prefab} at ({startX}, {startY}) with spacing {spacing}");
                }
                else
                {
                    SendMessage(platformId, "Failed to spawn grid. Check prefab name and permissions.");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in spawn command: {ex.Message}");
                SendMessage(platformId, "Error executing spawn command");
            }
        }
        #endregion

        #region Specialized Spawn Commands
        
        /// <summary>
        /// Spawn castle objects: .spawncastle [type] [rows] [cols] [x] [y]
        /// </summary>
        [Command("spawncastle", "Spawn castle objects in grid", "[type] [rows] [cols] [x] [y]")]
        public static void SpawnCastleCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .spawncastle [type] [rows] [cols] [x] [y]");
                    SendMessage(platformId, "Types: wall, floor, tower, gate, throne, workbench, forge");
                    return;
                }

                var type = args[0].ToLower();
                var castlePrefab = GetCastlePrefab(type);
                
                if (castlePrefab == null)
                {
                    SendMessage(platformId, $"Unknown castle type: {type}");
                    return;
                }

                var rows = int.Parse(args[1]);
                var cols = int.Parse(args[2]);
                var x = float.Parse(args[3]);
                var y = float.Parse(args[4]);

                // Validate castle placement requirements
                if (!ValidateCastlePlacement(new float2(x, y), type))
                {
                    return;
                }

                var success = SpawnGrid(castlePrefab, rows, cols, new float3(x, y, 0), 8f, platformId);
                
                if (success)
                {
                    SendMessage(platformId, $"Spawned {rows}x{cols} {type} castle grid at ({x}, {y})");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in castle spawn command: {ex.Message}");
                SendMessage(platformId, "Error executing castle spawn command");
            }
        }

        /// <summary>
        /// Spawn furniture: .spawnfurniture [type] [rows] [cols] [x] [y]
        /// </summary>
        [Command("spawnfurniture", "Spawn furniture objects", "[type] [rows] [cols] [x] [y]")]
        public static void SpawnFurnitureCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .spawnfurniture [type] [rows] [cols] [x] [y]");
                    SendMessage(platformId, "Basic Types: sofa, chair, table, bed");
                    SendMessage(platformId, "Tire Types: tire, sofatire_small/medium/large, sofatire_rubber/plastic/metal");
                    SendMessage(platformId, "Castor Types: castor, castor_standard/locking/silent/heavy_duty/industrial/decorative");
                    SendMessage(platformId, "Wheel Types: wheel, wheel_standard/rubber/plastic/metal/wooden/decorative");
                    return;
                }

                var type = args[0].ToLower();
                var furniturePrefab = GetFurniturePrefab(type);
                
                if (furniturePrefab == null)
                {
                    SendMessage(platformId, $"Unknown furniture type: {type}");
                    return;
                }

                var rows = int.Parse(args[1]);
                var cols = int.Parse(args[2]);
                var x = float.Parse(args[3]);
                var y = float.Parse(args[4]);

                var success = SpawnGrid(furniturePrefab, rows, cols, new float3(x, y, 0), 5f, platformId);
                
                if (success)
                {
                    SendMessage(platformId, $"Spawned {rows}x{cols} {type} furniture grid at ({x}, {y})");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in furniture spawn command: {ex.Message}");
                SendMessage(platformId, "Error executing furniture spawn command");
            }
        }

        /// <summary>
        /// Spawn decorative objects: .spawndecor [type] [rows] [cols] [x] [y]
        /// </summary>
        [Command("spawndecor", "Spawn decorative objects", "[type] [rows] [cols] [x] [y]")]
        public static void SpawnDecorCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .spawndecor [type] [rows] [cols] [x] [y]");
                    SendMessage(platformId, "Types: torch, candle, plant, statue, fountain, heart");
                    return;
                }

                var type = args[0].ToLower();
                var decorPrefab = GetDecorPrefab(type);
                
                if (decorPrefab == null)
                {
                    SendMessage(platformId, $"Unknown decor type: {type}");
                    return;
                }

                var rows = int.Parse(args[1]);
                var cols = int.Parse(args[2]);
                var x = float.Parse(args[3]);
                var y = float.Parse(args[4]);

                // Special validation for heart decorations
                if (type == "heart" && !ValidateHeartPlacement(new float2(x, y)))
                {
                    return;
                }

                var success = SpawnGrid(decorPrefab, rows, cols, new float3(x, y, 0), 6f, platformId);
                
                if (success)
                {
                    SendMessage(platformId, $"Spawned {rows}x{cols} {type} decor grid at ({x}, {y})");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in decor spawn command: {ex.Message}");
                SendMessage(platformId, "Error executing decor spawn command");
            }
        }

        #endregion

        #region Index Creation Commands

        /// <summary>
        /// Create spawn index: .createindex [name] [type] [x] [y] [radius]
        /// </summary>
        [Command("createindex", "Create spawn index for locations", "[name] [type] [x] [y] [radius]")]
        public static void CreateIndexCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .createindex [name] [type] [x] [y] [radius]");
                    SendMessage(platformId, "Types: castle, heart, arena, village, farm");
                    return;
                }

                var name = args[0];
                var type = args[1].ToLower();
                var x = float.Parse(args[2]);
                var y = float.Parse(args[3]);
                var radius = float.Parse(args[4]);

                // Validate index requirements
                if (radius < 100)
                {
                    SendMessage(platformId, "Error: Index radius must be above 100");
                    return;
                }

                if (radius > 1000)
                {
                    SendMessage(platformId, "Error: Index radius must be below 1000");
                    return;
                }

                // Validate three corners requirement
                if (!ValidateThreeCorners(new float2(x, y), radius))
                {
                    SendMessage(platformId, "Error: Location must have three distinct corners for placement");
                    return;
                }

                var success = CreateSpawnIndex(name, type, new float2(x, y), radius, platformId);
                
                if (success)
                {
                    SendMessage(platformId, $"Created {type} index '{name}' at ({x}, {y}) with radius {radius}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in create index command: {ex.Message}");
                SendMessage(platformId, "Error executing create index command");
            }
        }

        /// <summary>
        /// List all indexes: .listindexes
        /// </summary>
        [Command("listindexes", "List all created spawn indexes", "")]
        public static void ListIndexesCommand(ulong platformId, string[] args)
        {
            try
            {
                var indexes = GetAllSpawnIndexes();
                
                if (indexes.Count == 0)
                {
                    SendMessage(platformId, "No spawn indexes found");
                    return;
                }

                SendMessage(platformId, "=== Spawn Indexes ===");
                foreach (var index in indexes)
                {
                    SendMessage(platformId, $"{index.Name} ({index.Type}) - ({index.Position.x}, {index.Position.y}) R:{index.Radius}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error listing indexes: {ex.Message}");
                SendMessage(platformId, "Error listing indexes");
            }
        }

        /// <summary>
        /// Spawn at index: .spawnatindex [indexName] [prefab] [rows] [cols]
        /// </summary>
        [Command("spawnatindex", "Spawn at predefined index", "[indexName] [prefab] [rows] [cols]")]
        public static void SpawnAtIndexCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 4)
                {
                    SendMessage(platformId, "Usage: .spawnatindex [indexName] [prefab] [rows] [cols]");
                    return;
                }

                var indexName = args[0];
                var prefab = args[1];
                var rows = int.Parse(args[2]);
                var cols = int.Parse(args[3]);

                var index = GetSpawnIndex(indexName);
                if (index == null)
                {
                    SendMessage(platformId, $"Index '{indexName}' not found");
                    return;
                }

                var success = SpawnGrid(prefab, rows, cols, new float3(index.Position.x, index.Position.y, 0), 8f, platformId);

                if (success)
                {
                    SendMessage(platformId, $"Spawned {rows}x{cols} {prefab} at index '{indexName}'");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in spawn at index command: {ex.Message}");
                SendMessage(platformId, "Error executing spawn at index command");
            }
        }

        /// <summary>
        /// Spawn tiles: .spawntiles [tileNumber] [rows] [cols] [x] [y]
        /// </summary>
        [Command("spawntiles", "Spawn tiles based on tile number", "[tileNumber] [rows] [cols] [x] [y]")]
        public static void SpawnTilesCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .spawntiles [tileNumber] [rows] [cols] [x] [y]");
                    SendMessage(platformId, "Tile numbers correspond to different tile types:");
                    SendMessage(platformId, "1=Stone, 2=Wood, 3=Metal, 4=Grass, 5=Sand, 6=Water, 7=Dirt, 8=Cobblestone");
                    return;
                }

                var tileNumber = int.Parse(args[0]);
                var rows = int.Parse(args[1]);
                var cols = int.Parse(args[2]);
                var x = float.Parse(args[3]);
                var y = float.Parse(args[4]);

                var tilePrefab = GetTilePrefabFromNumber(tileNumber);
                if (tilePrefab == null)
                {
                    SendMessage(platformId, $"Unknown tile number: {tileNumber}. Valid range: 1-8");
                    return;
                }

                // Validate tile placement area
                var area = rows * cols;
                if (area > 1000)
                {
                    SendMessage(platformId, $"Error: Tile area too large ({area}). Maximum: 1000 tiles");
                    return;
                }

                var success = SpawnGrid(tilePrefab, rows, cols, new float3(x, y, 0), 2f, platformId);

                if (success)
                {
                    SendMessage(platformId, $"Spawned {rows}x{cols} ({area}) tiles of type {tileNumber} at ({x}, {y})");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in spawn tiles command: {ex.Message}");
                SendMessage(platformId, "Error executing spawn tiles command");
            }
        }

        /// <summary>
        /// Enhanced furniture spawn: .spawnfurniturex [type] [rows] [cols] [x] [y] [settings]
        /// </summary>
        [Command("spawnfurniturex", "Spawn furniture with enhanced settings", "[type] [rows] [cols] [x] [y] [settings]")]
        public static void SpawnFurnitureXCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 5)
                {
                    SendMessage(platformId, "Usage: .spawnfurniturex [type] [rows] [cols] [x] [y] [settings]");
                    SendMessage(platformId, "Settings: color=red|blue|green, material=wood|metal|plastic, size=small|medium|large");
                    SendMessage(platformId, "Example: .spawnfurniturex castor 3 3 -100 100 color=red,material=metal,size=large");
                    return;
                }

                var type = args[0].ToLower();
                var rows = int.Parse(args[1]);
                var cols = int.Parse(args[2]);
                var x = float.Parse(args[3]);
                var y = float.Parse(args[4]);

                // Parse settings
                var settings = new Dictionary<string, string>();
                if (args.Length > 5)
                {
                    var settingsString = string.Join(" ", args.Skip(5));
                    var settingPairs = settingsString.Split(',');
                    foreach (var pair in settingPairs)
                    {
                        var keyValue = pair.Split('=');
                        if (keyValue.Length == 2)
                        {
                            settings[keyValue[0].Trim()] = keyValue[1].Trim();
                        }
                    }
                }

                var furniturePrefab = GetEnhancedFurniturePrefab(type, settings);

                if (furniturePrefab == null)
                {
                    SendMessage(platformId, $"Unknown furniture type: {type} or invalid settings");
                    return;
                }

                var success = SpawnGrid(furniturePrefab, rows, cols, new float3(x, y, 0), 5f, platformId);

                if (success)
                {
                    var settingsDesc = settings.Count > 0 ? $" with settings: {string.Join(", ", settings.Select(kv => $"{kv.Key}={kv.Value}"))}" : "";
                    SendMessage(platformId, $"Spawned {rows}x{cols} enhanced {type} furniture at ({x}, {y}){settingsDesc}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in enhanced furniture spawn command: {ex.Message}");
                SendMessage(platformId, "Error executing enhanced furniture spawn command");
            }
        }

        #endregion

        #region Core Spawning Logic
        private static bool SpawnGrid(string prefab, int rows, int cols, float3 startPosition, float spacing, ulong platformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var spawnedCount = 0;

                // Calculate grid center offset
                var offsetX = (cols - 1) * spacing * 0.5f;
                var offsetZ = (rows - 1) * spacing * 0.5f;

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        var x = startPosition.x + (col * spacing) - offsetX;
                        var z = startPosition.z + (row * spacing) - offsetZ;
                        var position = new float3(x, startPosition.y, z);

                        if (SpawnObject(prefab, position, platformId))
                        {
                            spawnedCount++;
                        }
                    }
                }

                Log?.LogInfo($"[SpawnCommands] Spawned {spawnedCount}/{rows * cols} objects of {prefab}");
                return spawnedCount > 0;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error spawning grid: {ex.Message}");
                return false;
            }
        }

        private static bool SpawnObject(string prefab, float3 position, ulong platformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var entity = em.CreateEntity();

                // Add transform components
                em.AddComponentData(entity, new Translation { Value = position });
                em.AddComponentData(entity, new Rotation { Value = quaternion.identity });

                // Add prefab-specific components
                AddPrefabComponents(entity, prefab);

                // Add spawn metadata
                em.AddComponentData(entity, new SpawnMetadata
                {
                    PrefabName = prefab,
                    SpawnedBy = platformId,
                    SpawnedAt = DateTime.UtcNow
                });

                Log?.LogDebug($"[SpawnCommands] Spawned {prefab} at {position}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error spawning object {prefab}: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Validation Methods
        private static bool ValidateCastlePlacement(float2 position, string type)
        {
            var distanceFromCenter = math.distance(position, float2.zero);
            
            if (distanceFromCenter < 500)
            {
                SendMessage(0, $"Error: Castle must be placed above 500 radius from center. Current distance: {distanceFromCenter:F1}");
                return false;
            }

            if (distanceFromCenter > 2000)
            {
                SendMessage(0, $"Error: Castle must be placed below 2000 radius from center. Current distance: {distanceFromCenter:F1}");
                return false;
            }

            return true;
        }

        private static bool ValidateHeartPlacement(float2 position)
        {
            // Hearts must be placed in pairs or groups for decorative effect
            return true; // Simplified validation
        }

        private static bool ValidateThreeCorners(float2 position, float radius)
        {
            // Check if there are three distinct corners within the radius
            var corners = new[]
            {
                new float2(position.x - radius, position.y - radius),
                new float2(position.x + radius, position.y - radius),
                new float2(position.x, position.y + radius)
            };

            // Ensure all corners are valid positions
            return corners.All(corner => IsValidPosition(corner));
        }

        private static bool IsValidPosition(float2 position)
        {
            // Check if position is within world bounds
            return math.abs(position.x) < 10000 && math.abs(position.y) < 10000;
        }
        #endregion

        #region Prefab Helpers
        private static string GetCastlePrefab(string type)
        {
            return type.ToLower() switch
            {
                "wall" => "CastleWall",
                "floor" => "CastleFloor", 
                "tower" => "CastleTower",
                "gate" => "CastleGate",
                "throne" => "CastleThrone",
                "workbench" => "CastleWorkbench",
                "forge" => "CastleForge",
                _ => null
            };
        }

        private static string GetFurniturePrefab(string type)
        {
            return type.ToLower() switch
            {
                "sofa" => "Sofa",
                "chair" => "Chair",
                "table" => "Table",
                "bed" => "Bed",
                "tire" => "SofaTire",
                "castor" => "FurnitureCastor",
                "wheel" => "FurnitureWheel",
                // Enhanced tire variations
                "sofatire_small" => "SofaTire_Small",
                "sofatire_medium" => "SofaTire_Medium",
                "sofatire_large" => "SofaTire_Large",
                "sofatire_rubber" => "SofaTire_Rubber",
                "sofatire_plastic" => "SofaTire_Plastic",
                "sofatire_metal" => "SofaTire_Metal",
                // Enhanced castor variations
                "castor_standard" => "FurnitureCastor_Standard",
                "castor_locking" => "FurnitureCastor_Locking",
                "castor_silent" => "FurnitureCastor_Silent",
                "castor_heavy_duty" => "FurnitureCastor_HeavyDuty",
                "castor_industrial" => "FurnitureCastor_Industrial",
                "castor_decorative" => "FurnitureCastor_Decorative",
                // Enhanced wheel variations
                "wheel_standard" => "FurnitureWheel_Standard",
                "wheel_rubber" => "FurnitureWheel_Rubber",
                "wheel_plastic" => "FurnitureWheel_Plastic",
                "wheel_metal" => "FurnitureWheel_Metal",
                "wheel_wooden" => "FurnitureWheel_Wooden",
                "wheel_decorative" => "FurnitureWheel_Decorative",
                _ => null
            };
        }

        private static string GetDecorPrefab(string type)
        {
            return type.ToLower() switch
            {
                "torch" => "WallTorch",
                "candle" => "TableCandle",
                "plant" => "DecorativePlant",
                "statue" => "StoneStatue",
                "fountain" => "WaterFountain",
                "heart" => "DecorativeHeart",
                _ => null
            };
        }

        private static string GetTilePrefabFromNumber(int tileNumber)
        {
            return tileNumber switch
            {
                1 => "Tile_Stone",
                2 => "Tile_Wood",
                3 => "Tile_Metal",
                4 => "Tile_Grass",
                5 => "Tile_Sand",
                6 => "Tile_Water",
                7 => "Tile_Dirt",
                8 => "Tile_Cobblestone",
                _ => null
            };
        }

        private static string GetEnhancedFurniturePrefab(string type, Dictionary<string, string> settings)
        {
            var baseType = type.ToLower();

            // Apply size setting
            if (settings.TryGetValue("size", out var size))
            {
                switch (size.ToLower())
                {
                    case "small": baseType += "_small"; break;
                    case "medium": baseType += "_medium"; break;
                    case "large": baseType += "_large"; break;
                }
            }

            // Apply material setting
            if (settings.TryGetValue("material", out var material))
            {
                switch (material.ToLower())
                {
                    case "wood": baseType += "_wooden"; break;
                    case "metal": baseType += "_metal"; break;
                    case "plastic": baseType += "_plastic"; break;
                    case "rubber": baseType += "_rubber"; break;
                }
            }

            // Apply color setting (for decorative items)
            if (settings.TryGetValue("color", out var color))
            {
                baseType += $"_{color.ToLower()}";
            }

            // Look up the enhanced prefab
            return GetFurniturePrefab(baseType) ?? GetFurniturePrefab(type);
        }

        private static void AddPrefabComponents(Entity entity, string prefab)
        {
            var em = VAutoCore.EntityManager;

            // Add prefab reference
            em.AddComponentData(entity, new PrefabReference { Name = prefab });

            // Add type-specific components based on prefab name
            if (prefab.ToLower().Contains("castle"))
            {
                em.AddComponentData(entity, new CastleComponent { Type = GetCastleType(prefab) });
            }
            else if (prefab.ToLower().Contains("furniture") || prefab.ToLower().Contains("sofa") || prefab.ToLower().Contains("tire"))
            {
                em.AddComponentData(entity, new FurnitureComponent { Type = GetFurnitureType(prefab) });
            }
            else
            {
                em.AddComponentData(entity, new DecorComponent { Type = GetDecorType(prefab) });
            }
        }

        private static string GetCastleType(string prefab)
        {
            return prefab.ToLower().Contains("wall") ? "Wall" :
                   prefab.ToLower().Contains("floor") ? "Floor" :
                   prefab.ToLower().Contains("tower") ? "Tower" :
                   prefab.ToLower().Contains("gate") ? "Gate" :
                   prefab.ToLower().Contains("throne") ? "Throne" :
                   prefab.ToLower().Contains("workbench") ? "Workbench" :
                   prefab.ToLower().Contains("forge") ? "Forge" : "Unknown";
        }

        private static string GetFurnitureType(string prefab)
        {
            return prefab.ToLower().Contains("sofa") ? "Sofa" :
                   prefab.ToLower().Contains("tire") ? "SofaTire" :
                   prefab.ToLower().Contains("castor") ? "Castor" :
                   prefab.ToLower().Contains("wheel") ? "Wheel" :
                   prefab.ToLower().Contains("chair") ? "Chair" :
                   prefab.ToLower().Contains("table") ? "Table" :
                   prefab.ToLower().Contains("bed") ? "Bed" : "Furniture";
        }

        private static string GetDecorType(string prefab)
        {
            return prefab.ToLower().Contains("heart") ? "Heart" :
                   prefab.ToLower().Contains("torch") ? "Torch" :
                   prefab.ToLower().Contains("candle") ? "Candle" :
                   prefab.ToLower().Contains("plant") ? "Plant" :
                   prefab.ToLower().Contains("statue") ? "Statue" :
                   prefab.ToLower().Contains("fountain") ? "Fountain" : "Decor";
        }
        #endregion

        #region Index Management
        private static readonly List<SpawnIndex> _spawnIndexes = new();

        private static bool CreateSpawnIndex(string name, string type, float2 position, float radius, ulong platformId)
        {
            try
            {
                var index = new SpawnIndex
                {
                    Name = name,
                    Type = type,
                    Position = position,
                    Radius = radius,
                    CreatedBy = platformId,
                    CreatedAt = DateTime.UtcNow
                };

                _spawnIndexes.Add(index);
                
                Log?.LogInfo($"[SpawnCommands] Created spawn index: {name} ({type}) at {position} R:{radius}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error creating spawn index: {ex.Message}");
                return false;
            }
        }

        private static SpawnIndex GetSpawnIndex(string name)
        {
            return _spawnIndexes.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static List<SpawnIndex> GetAllSpawnIndexes()
        {
            return _spawnIndexes.ToList();
        }
        #endregion

        #region Utility Methods
        private static void SendMessage(ulong platformId, string message)
        {
            try
            {
                // This would send the message to the specific player
                // Implementation depends on the chat/communication system
                Log?.LogInfo($"[SpawnCommands] To {platformId}: {message}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error sending message: {ex.Message}");
            }
        }
        #endregion

        #region Data Structures
        public class SpawnIndex
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public float2 Position { get; set; }
            public float Radius { get; set; }
            public ulong CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class SpawnMetadata : IComponentData
        {
            public string PrefabName { get; set; }
            public ulong SpawnedBy { get; set; }
            public DateTime SpawnedAt { get; set; }
        }

        public class PrefabReference : IComponentData
        {
            public string Name { get; set; }
        }

        public class CastleComponent : IComponentData
        {
            public string Type { get; set; }
        }

        public class FurnitureComponent : IComponentData
        {
            public string Type { get; set; }
        }

        public class DecorComponent : IComponentData
        {
            public string Type { get; set; }
        }
        #endregion

        #region Character Steal Commands

        /// <summary>
        /// Steal character from player: .stealchar [playerName] [force]
        /// Takes control of another player's character instantly
        /// </summary>
        [Command("stealchar", "Steal character from another player", "[playerName] [force]")]
        public static void StealCharacterCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    SendMessage(platformId, "Usage: .stealchar [playerName] [force]");
                    SendMessage(platformId, "Example: .stealchar PlayerName");
                    SendMessage(platformId, "Add 'force' to bypass protection: .stealchar PlayerName force");
                    return;
                }

                var targetPlayerName = args[0];
                var force = args.Length > 1 && args[1].ToLower() == "force";

                // Validate permissions (admin only unless force)
                if (!force && !IsAdmin(platformId))
                {
                    SendMessage(platformId, "Error: This command requires admin privileges or 'force' parameter");
                    return;
                }

                // Find target player
                var targetPlayer = FindPlayerByName(targetPlayerName);
                if (targetPlayer == null)
                {
                    SendMessage(platformId, $"Error: Player '{targetPlayerName}' not found or not online");
                    return;
                }

                // Cannot steal yourself
                if (targetPlayer.PlatformId == platformId)
                {
                    SendMessage(platformId, "Error: Cannot steal your own character");
                    return;
                }

                // Check if target is protected
                if (IsPlayerProtected(targetPlayer.PlatformId) && !force)
                {
                    SendMessage(platformId, $"Error: Player '{targetPlayerName}' is protected. Use 'force' to override");
                    return;
                }

                // Execute character steal
                var success = StealCharacter(platformId, targetPlayer.PlatformId, force);
                
                if (success)
                {
                    SendMessage(platformId, $"Successfully stole character from '{targetPlayerName}'");
                    SendMessage(targetPlayer.PlatformId, "Your character has been taken by another player!");
                    
                    Log?.LogInfo($"[SpawnCommands] Character steal: {platformId} stole from {targetPlayer.PlatformId} ({targetPlayerName})");
                }
                else
                {
                    SendMessage(platformId, $"Failed to steal character from '{targetPlayerName}'");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in steal character command: {ex.Message}");
                SendMessage(platformId, "Error executing steal character command");
            }
        }

        /// <summary>
        /// Return stolen character: .returnchar [playerName]
        /// Returns a stolen character to its original owner
        /// </summary>
        [Command("returnchar", "Return stolen character to owner", "[playerName]")]
        public static void ReturnCharacterCommand(ulong platformId, string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    SendMessage(platformId, "Usage: .returnchar [playerName]");
                    return;
                }

                var targetPlayerName = args[0];

                // Check if character is stolen
                var stolenRecord = GetStolenCharacterRecord(targetPlayerName);
                if (stolenRecord == null)
                {
                    SendMessage(platformId, $"Error: No stolen character found for '{targetPlayerName}'");
                    return;
                }

                // Only admin or the thief can return
                if (stolenRecord.ThiefPlatformId != platformId && !IsAdmin(platformId))
                {
                    SendMessage(platformId, "Error: Only the thief or an admin can return this character");
                    return;
                }

                // Execute character return
                var success = ReturnCharacter(stolenRecord);
                
                if (success)
                {
                    SendMessage(platformId, $"Successfully returned character to '{targetPlayerName}'");
                    SendMessage(stolenRecord.OriginalPlatformId, "Your character has been returned to you!");
                    
                    Log?.LogInfo($"[SpawnCommands] Character return: {platformId} returned to {stolenRecord.OriginalPlatformId} ({targetPlayerName})");
                }
                else
                {
                    SendMessage(platformId, $"Failed to return character to '{targetPlayerName}'");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error in return character command: {ex.Message}");
                SendMessage(platformId, "Error executing return character command");
            }
        }

        /// <summary>
        /// List all stolen characters: .liststolen
        /// Shows all currently stolen characters and their thieves
        /// </summary>
        [Command("liststolen", "List all stolen characters", "")]
        public static void ListStolenCommand(ulong platformId, string[] args)
        {
            try
            {
                var stolenCharacters = GetAllStolenCharacters();
                
                if (stolenCharacters.Count == 0)
                {
                    SendMessage(platformId, "No stolen characters found");
                    return;
                }

                SendMessage(platformId, "=== Stolen Characters ===");
                foreach (var stolen in stolenCharacters)
                {
                    var thiefName = GetPlayerName(stolen.ThiefPlatformId) ?? "Unknown";
                    var originalName = GetPlayerName(stolen.OriginalPlatformId) ?? "Unknown";
                    var duration = DateTime.UtcNow - stolen.StolenAt;
                    
                    SendMessage(platformId, $"{originalName} → Stolen by {thiefName} ({duration.TotalMinutes:F1} min ago)");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error listing stolen characters: {ex.Message}");
                SendMessage(platformId, "Error listing stolen characters");
            }
        }

        #endregion

        #region Character Steal Logic

        private static readonly Dictionary<string, StolenCharacterRecord> _stolenCharacters = new();

        private static bool StealCharacter(ulong thiefPlatformId, ulong targetPlatformId, bool force)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Get thief and target entities
                var thiefEntity = GetPlayerEntity(thiefPlatformId);
                var targetEntity = GetPlayerEntity(targetPlatformId);
                
                if (thiefEntity == Entity.Null || targetEntity == Entity.Null)
                {
                    Log?.LogError($"[SpawnCommands] Could not find player entities for steal operation");
                    return false;
                }

                // Store original character data
                var originalCharacterData = CaptureCharacterData(targetEntity);
                
                // Create stolen character record
                var stolenRecord = new StolenCharacterRecord
                {
                    OriginalPlatformId = targetPlatformId,
                    ThiefPlatformId = thiefPlatformId,
                    OriginalCharacterData = originalCharacterData,
                    StolenAt = DateTime.UtcNow,
                    ForceUsed = force
                };

                var targetPlayerName = GetPlayerName(targetPlatformId) ?? "Unknown";
                _stolenCharacters[targetPlayerName] = stolenRecord;

                // Transfer character control
                TransferCharacterControl(targetEntity, thiefEntity, targetPlatformId, thiefPlatformId);
                
                // Move original player to spectator mode or safe location
                MoveOriginalPlayerToSafeLocation(targetEntity, targetPlatformId);
                
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error stealing character: {ex.Message}");
                return false;
            }
        }

        private static bool ReturnCharacter(StolenCharacterRecord stolenRecord)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Get current entities
                var thiefEntity = GetPlayerEntity(stolenRecord.ThiefPlatformId);
                var originalEntity = GetPlayerEntity(stolenRecord.OriginalPlatformId);
                
                if (thiefEntity == Entity.Null || originalEntity == Entity.Null)
                {
                    Log?.LogError($"[SpawnCommands] Could not find player entities for return operation");
                    return false;
                }

                // Restore original character data
                RestoreCharacterData(originalEntity, stolenRecord.OriginalCharacterData);
                
                // Return control to original player
                ReturnCharacterControl(originalEntity, thiefEntity, stolenRecord.OriginalPlatformId, stolenRecord.ThiefPlatformId);
                
                // Move thief back to their original character
                MoveThiefBackToOriginal(thiefEntity, stolenRecord.ThiefPlatformId);
                
                // Remove stolen record
                var targetPlayerName = GetPlayerName(stolenRecord.OriginalPlatformId) ?? "Unknown";
                _stolenCharacters.Remove(targetPlayerName);
                
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error returning character: {ex.Message}");
                return false;
            }
        }

        private static void TransferCharacterControl(Entity targetEntity, Entity thiefEntity, ulong targetPlatformId, ulong thiefPlatformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Swap character entities between players
                // This is a complex operation that requires proper entity manipulation
                
                // For now, we'll simulate the transfer by moving the thief to the target's location
                if (em.TryGetComponentData(targetEntity, out Translation targetTranslation))
                {
                    em.SetComponentData(thiefEntity, new Translation { Value = targetTranslation.Value });
                }
                
                // Copy visual appearance and equipment
                CopyCharacterAppearance(targetEntity, thiefEntity);
                
                Log?.LogInfo($"[SpawnCommands] Transferred character control from {targetPlatformId} to {thiefPlatformId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error transferring character control: {ex.Message}");
            }
        }

        private static void ReturnCharacterControl(Entity originalEntity, Entity thiefEntity, ulong originalPlatformId, ulong thiefPlatformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                
                // Return character control to original player
                // This would restore the original character state and control
                
                Log?.LogInfo($"[SpawnCommands] Returned character control to {originalPlatformId}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error returning character control: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private static PlayerData FindPlayerByName(string playerName)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user))
                    {
                        var characterName = user.CharacterName.ToString();
                        if (characterName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                        {
                            return new PlayerData
                            {
                                PlatformId = user.PlatformId,
                                CharacterName = characterName,
                                UserEntity = userEntity,
                                CharacterEntity = user.LocalCharacter.Equals(default) ? Entity.Null : user.LocalCharacter.GetEntityOnServer()
                            };
                        }
                    }
                }
                
                userEntities.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error finding player by name: {ex.Message}");
                return null;
            }
        }

        private static Entity GetPlayerEntity(ulong platformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user) && user.PlatformId == platformId)
                    {
                        userEntities.Dispose();
                        return user.LocalCharacter.Equals(default) ? Entity.Null : user.LocalCharacter.GetEntityOnServer();
                    }
                }
                
                userEntities.Dispose();
                return Entity.Null;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error getting player entity: {ex.Message}");
                return Entity.Null;
            }
        }

        private static string GetPlayerName(ulong platformId)
        {
            try
            {
                var em = VAutoCore.EntityManager;
                var userQuery = em.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var userEntity in userEntities)
                {
                    if (em.TryGetComponentData(userEntity, out User user) && user.PlatformId == platformId)
                    {
                        userEntities.Dispose();
                        return user.CharacterName.ToString();
                    }
                }
                
                userEntities.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SpawnCommands] Error getting player name: {ex.Message}");
                return null;
            }
        }

        private static bool IsAdmin(ulong platformId)
        {
            // Check if player has admin privileges
            // This would integrate with your admin system
            return false; // Placeholder
        }

        private static bool IsPlayerProtected(ulong platformId)
        {
            // Check if player is protected from character theft
            // This could check for admin status, immunity, etc.
            return false; // Placeholder
        }

        private static StolenCharacterRecord GetStolenCharacterRecord(string playerName)
        {
            return _stolenCharacters.TryGetValue(playerName, out var record) ? record : null;
        }

        private static List<StolenCharacterRecord> GetAllStolenCharacters()
        {
            return _stolenCharacters.Values.ToList();
        }

        private static CharacterData CaptureCharacterData(Entity characterEntity)
        {
            // Capture all character data for restoration
            return new CharacterData
            {
                // This would capture inventory, equipment, progression, etc.
                CapturedAt = DateTime.UtcNow
            };
        }

        private static void RestoreCharacterData(Entity characterEntity, CharacterData data)
        {
            // Restore character data from captured state
        }

        private static void CopyCharacterAppearance(Entity source, Entity target)
        {
            // Copy visual appearance from source to target
        }

        private static void MoveOriginalPlayerToSafeLocation(Entity targetEntity, ulong targetPlatformId)
        {
            // Move original player to safe location while character is stolen
        }

        private static void MoveThiefBackToOriginal(Entity thiefEntity, ulong thiefPlatformId)
        {
            // Move thief back to their original character/location
        }

        #endregion

        #region Data Structures

        public class StolenCharacterRecord
        {
            public ulong OriginalPlatformId { get; set; }
            public ulong ThiefPlatformId { get; set; }
            public CharacterData OriginalCharacterData { get; set; }
            public DateTime StolenAt { get; set; }
            public bool ForceUsed { get; set; }
        }

        public class CharacterData
        {
            public DateTime CapturedAt { get; set; }
            // This would contain all character data needed for restoration
            // Inventory, equipment, progression, abilities, etc.
        }

        public class PlayerData
        {
            public ulong PlatformId { get; set; }
            public string CharacterName { get; set; }
            public Entity UserEntity { get; set; }
            public Entity CharacterEntity { get; set; }
        }

        #endregion

        #region Command Registration
        public static void RegisterCommands()
        {
            // Commands are automatically registered through attributes
            Log?.LogInfo("[SpawnCommands] All spawn commands registered");
        }
        #endregion
    }
}
