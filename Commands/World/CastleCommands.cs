using System;
using System.Linq;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Services.Systems;
using VAuto.Services.World;
using BepInEx.Logging;

namespace VAuto.Commands.World
{
    /// <summary>
    /// Castle object commands - integration with castle building system.
    /// Manages castle objects within the world automation framework.
    /// </summary>
    public static class CastleCommands
    {
        #region Castle Object Management
        [Command("castlespawn", "Spawn castle objects", "<type> <name> [x] [y] [z]")]
        public static void SpawnCastleCommand(ChatCommandContext ctx, string type, string name, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                var position = new float3(x, y, z);
                var castleType = ParseCastleType(type);

                // Spawn the castle object using world spawn service
                var entity = SpawnCastleObject(castleType, name, position);

                if (entity != Entity.Null)
                {
                    // Register with castle integration service
                    CastleObjectIntegrationService.RegisterCastleObject(entity, castleType, name);
                    ctx.Reply($"Spawned castle {type} '{name}' at ({x}, {y}, {z})");
                }
                else
                {
                    ctx.Reply($"Failed to spawn castle {type} '{name}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Spawn error: {ex.Message}");
                ctx.Reply("Error spawning castle object");
            }
        }

        [Command("remove", "Remove castle objects", "[name]")]
        public static void RemoveCastleCommand(ChatCommandContext ctx, string name = "")
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    // Remove nearest castle object
                    var character = ctx.Event.SenderCharacterEntity;
                    var position = VRCore.EntityManager.GetComponentData<Translation>(character).Value;
                    
                    var nearest = CastleObjectIntegrationService.GetCastleObjectsInRange(position, 10f)
                        .OrderBy(o => math.distance(o.Position, position))
                        .FirstOrDefault();

                    if (nearest != null)
                    {
                        CastleObjectIntegrationService.UnregisterCastleObject(nearest.Entity);
                        WorldObjectService.Remove(nearest.Entity);
                        ctx.Reply($"Removed nearest castle object: {nearest.Name}");
                    }
                    else
                    {
                        ctx.Reply("No castle objects found nearby");
                    }
                }
                else
                {
                    // Remove specific castle object by name
                    var objects = CastleObjectIntegrationService.GetAllCastleObjects()
                        .Where(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var obj in objects)
                    {
                        CastleObjectIntegrationService.UnregisterCastleObject(obj.Entity);
                        WorldObjectService.Remove(obj.Entity);
                    }

                    ctx.Reply($"Removed {objects.Count} castle objects named '{name}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Remove error: {ex.Message}");
                ctx.Reply("Error removing castle object");
            }
        }

        [Command("list", "List castle objects", "[type]")]
        public static void ListCastleCommand(ChatCommandContext ctx, string type = "")
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    // List all castle objects
                    var allObjects = CastleObjectIntegrationService.GetAllCastleObjects();
                    ctx.Reply($"Total castle objects: {allObjects.Count}");

                    foreach (var obj in allObjects.Take(20))
                    {
                        ctx.Reply($"  {obj.Type} '{obj.Name}' at ({obj.Position.x:F1}, {obj.Position.y:F1}, {obj.Position.z:F1}) - {(obj.IsActive ? "Active" : "Inactive")}");
                    }

                    if (allObjects.Count > 20)
                    {
                        ctx.Reply($"  ... and {allObjects.Count - 20} more");
                    }
                }
                else
                {
                    // List specific type
                    var castleType = ParseCastleType(type);
                    var objects = CastleObjectIntegrationService.GetCastleObjectsByType(castleType);
                    
                    ctx.Reply($"{type} castle objects: {objects.Count}");
                    foreach (var obj in objects)
                    {
                        ctx.Reply($"  '{obj.Name}' at ({obj.Position.x:F1}, {obj.Position.y:F1}, {obj.Position.z:F1}) - {(obj.IsActive ? "Active" : "Inactive")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] List error: {ex.Message}");
                ctx.Reply("Error listing castle objects");
            }
        }

        [Command("activate", "Activate/deactivate castle objects", "<name> <true|false>")]
        public static void ActivateCastleCommand(ChatCommandContext ctx, string name, bool active)
        {
            try
            {
                var objects = CastleObjectIntegrationService.GetAllCastleObjects()
                    .Where(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var obj in objects)
                {
                    CastleObjectIntegrationService.SetCastleObjectActive(obj.Entity, active);
                }

                ctx.Reply($"{(active ? "Activated" : "Deactivated")} {objects.Count} castle objects named '{name}'");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Activate error: {ex.Message}");
                ctx.Reply("Error activating castle object");
            }
        }
        #endregion

        #region Arena Integration
        [Command("toarena", "Transfer castle objects to arena", "<name>")]
        public static void ToArenaCommand(ChatCommandContext ctx, string name)
        {
            try
            {
                var objects = CastleObjectIntegrationService.GetAllCastleObjects()
                    .Where(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!objects.Any())
                {
                    ctx.Reply($"No castle objects found named '{name}'");
                    return;
                }

                var arenaPosition = new float3(-1000f, 5f, -500f); // Default arena center
                var transferred = 0;

                foreach (var obj in objects)
                {
                    if (CastleObjectIntegrationService.TransferObjectToArena(obj.Entity, arenaPosition))
                    {
                        transferred++;
                    }
                }

                ctx.Reply($"Transferred {transferred} castle objects to arena");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] ToArena error: {ex.Message}");
                ctx.Reply("Error transferring to arena");
            }
        }

        [Command("fromarena", "Return castle objects from arena", "[name]")]
        public static void FromArenaCommand(ChatCommandContext ctx, string name = "")
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    // Return all arena objects
                    var arenaObjects = CastleObjectIntegrationService.GetArenaCastleObjects();
                    var returned = 0;

                    foreach (var entity in arenaObjects)
                    {
                        if (CastleObjectIntegrationService.ReturnObjectToCastle(entity))
                        {
                            returned++;
                        }
                    }

                    ctx.Reply($"Returned {returned} castle objects from arena");
                }
                else
                {
                    // Return specific objects
                    var objects = CastleObjectIntegrationService.GetAllCastleObjects()
                        .Where(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && o.IsInArena)
                        .ToList();

                    var returned = 0;
                    foreach (var obj in objects)
                    {
                        if (CastleObjectIntegrationService.ReturnObjectToCastle(obj.Entity))
                        {
                            returned++;
                        }
                    }

                    ctx.Reply($"Returned {returned} castle objects named '{name}' from arena");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] FromArena error: {ex.Message}");
                ctx.Reply("Error returning from arena");
            }
        }

        [Command("arena", "Show arena castle objects")]
        public static void ArenaCommand(ChatCommandContext ctx)
        {
            try
            {
                var arenaObjects = CastleObjectIntegrationService.GetArenaCastleObjects();
                ctx.Reply($"Castle objects in arena: {arenaObjects.Count}");

                foreach (var entity in arenaObjects)
                {
                    var obj = CastleObjectIntegrationService.GetCastleObject(entity);
                    if (obj != null)
                    {
                        ctx.Reply($"  {obj.Type} '{obj.Name}' at ({obj.Position.x:F1}, {obj.Position.y:F1}, {obj.Position.z:F1})");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Arena error: {ex.Message}");
                ctx.Reply("Error getting arena objects");
            }
        }
        #endregion

        #region Building Commands
        [Command("build", "Build castle structures", "<type> <rows> <cols> <centerX> <centerY> <centerZ> [spacing]")]
        public static void BuildCommand(ChatCommandContext ctx, string type, int rows, int cols, float centerX, float centerY, float centerZ, float spacing = 8f)
        {
            try
            {
                var castleType = ParseCastleType(type);
                var center = new float3(centerX, centerY, centerZ);
                var prefabName = GetCastlePrefab(castleType);

                if (string.IsNullOrEmpty(prefabName))
                {
                    ctx.Reply($"Unknown castle type: {type}");
                    return;
                }

                // Build grid of castle objects
                var entities = WorldSpawnService.SpawnGrid(prefabName, rows, cols, center, spacing, WorldObjectType.Structure);
                var registered = 0;

                foreach (var entity in entities)
                {
                    var position = VRCore.EntityManager.GetComponentData<Translation>(entity).Value;
                    var name = $"{type}_{registered + 1}";
                    
                    if (CastleObjectIntegrationService.RegisterCastleObject(entity, castleType, name))
                    {
                        registered++;
                    }
                }

                ctx.Reply($"Built {registered} {type} structures in {rows}x{cols} grid");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Build error: {ex.Message}");
                ctx.Reply("Error building castle structures");
            }
        }

        [Command("room", "Build a complete room", "<type> <width> <length> <centerX> <centerY> <centerZ>")]
        public static void RoomCommand(ChatCommandContext ctx, string type, int width, int length, float centerX, float centerY, float centerZ)
        {
            try
            {
                var castleType = ParseCastleType(type);
                var center = new float3(centerX, centerY, centerZ);
                var built = 0;
                var floorBuilt = 0;
                var wallBuilt = 0;

                // Build floor
                if (width > 0 && length > 0)
                {
                    var floorPrefab = GetFloorPrefab(castleType);
                    var floorEntities = WorldSpawnService.SpawnGrid(floorPrefab, width, length, center, 2f, WorldObjectType.Tile);
                    
                    foreach (var entity in floorEntities)
                    {
                        var name = $"floor_{floorBuilt + 1}";
                        if (CastleObjectIntegrationService.RegisterCastleObject(entity, CastleObjectIntegrationService.CastleObjectType.Storage, name))
                        {
                            floorBuilt++;
                            built++;
                        }
                    }
                    
                    ctx.Reply($"Built {floorBuilt} floor tiles");
                }

                // Build walls (perimeter) with proper wall prefabs
                var wallPrefab = GetWallPrefab(castleType);
                if (!string.IsNullOrEmpty(wallPrefab))
                {
                    var halfWidth = (width - 1) * 2f; // 2m spacing for walls
                    var halfLength = (length - 1) * 2f;
                    var wallHeight = 3f; // 3m high walls

                    // Top and bottom walls
                    for (int x = 0; x < width; x++)
                    {
                        var pos1 = center + new float3((x * 2f - halfWidth), 0, halfLength);
                        var pos2 = center + new float3((x * 2f - halfWidth), 0, -halfLength);
                        
                        // Build wall segments with height
                        var wall1 = BuildWallSegment(wallPrefab, pos1, wallHeight);
                        var wall2 = BuildWallSegment(wallPrefab, pos2, wallHeight);
                        
                        if (wall1 != Entity.Null)
                        {
                            CastleObjectIntegrationService.RegisterCastleObject(wall1, CastleObjectIntegrationService.CastleObjectType.Defense, $"wall_north_{wallBuilt++}");
                            built++;
                        }
                        if (wall2 != Entity.Null)
                        {
                            CastleObjectIntegrationService.RegisterCastleObject(wall2, CastleObjectIntegrationService.CastleObjectType.Defense, $"wall_south_{wallBuilt++}");
                            built++;
                        }
                    }

                    // Left and right walls (excluding corners to avoid duplication)
                    for (int z = 1; z < length - 1; z++)
                    {
                        var pos1 = center + new float3(halfWidth, 0, (z * 2f - halfLength));
                        var pos2 = center + new float3(-halfWidth, 0, (z * 2f - halfLength));
                        
                        var wall1 = BuildWallSegment(wallPrefab, pos1, wallHeight);
                        var wall2 = BuildWallSegment(wallPrefab, pos2, wallHeight);
                        
                        if (wall1 != Entity.Null)
                        {
                            CastleObjectIntegrationService.RegisterCastleObject(wall1, CastleObjectIntegrationService.CastleObjectType.Defense, $"wall_east_{wallBuilt++}");
                            built++;
                        }
                        if (wall2 != Entity.Null)
                        {
                            CastleObjectIntegrationService.RegisterCastleObject(wall2, CastleObjectIntegrationService.CastleObjectType.Defense, $"wall_west_{wallBuilt++}");
                            built++;
                        }
                    }
                    
                    ctx.Reply($"Built {wallBuilt} wall segments");
                }

                // Add corner pillars for structural support
                var pillarPrefab = GetPillarPrefab(castleType);
                if (!string.IsNullOrEmpty(pillarPrefab))
                {
                    var corners = new[]
                    {
                        center + new float3(-(width - 1) * 1f, 0, -(length - 1) * 1f), // SW
                        center + new float3((width - 1) * 1f, 0, -(length - 1) * 1f),  // SE
                        center + new float3((width - 1) * 1f, 0, (length - 1) * 1f),   // NE
                        center + new float3(-(width - 1) * 1f, 0, (length - 1) * 1f)   // NW
                    };
                    
                    var pillarNames = new[] { "pillar_sw", "pillar_se", "pillar_ne", "pillar_nw" };
                    var pillarsBuilt = 0;
                    
                    for (int i = 0; i < corners.Length; i++)
                    {
                        var pillar = WorldSpawnService.SpawnStructure(pillarPrefab, corners[i]);
                        if (pillar != Entity.Null)
                        {
                            CastleObjectIntegrationService.RegisterCastleObject(pillar, CastleObjectIntegrationService.CastleObjectType.Defense, pillarNames[i]);
                            pillarsBuilt++;
                            built++;
                        }
                    }
                    
                    ctx.Reply($"Built {pillarsBuilt} corner pillars");
                }

                ctx.Reply($"Built {type} room ({width}x{length}) with {built} total objects: {floorBuilt} floors, {wallBuilt} walls, {built - floorBuilt - wallBuilt} other");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[CastleCommands] Room error: {ex.Message}");
                ctx.Reply("Error building room");
            }
        }
        #endregion

        #region Utility Methods
        private static CastleObjectIntegrationService.CastleObjectType ParseCastleType(string type)
        {
            return type.ToLower() switch
            {
                "workbench" => CastleObjectIntegrationService.CastleObjectType.Workbench,
                "forge" => CastleObjectIntegrationService.CastleObjectType.Forge,
                "alchemy" => CastleObjectIntegrationService.CastleObjectType.AlchemyTable,
                "throne" => CastleObjectIntegrationService.CastleObjectType.Throne,
                "storage" => CastleObjectIntegrationService.CastleObjectType.Storage,
                "decoration" => CastleObjectIntegrationService.CastleObjectType.Decoration,
                "trap" => CastleObjectIntegrationService.CastleObjectType.Trap,
                "utility" => CastleObjectIntegrationService.CastleObjectType.Utility,
                "defense" => CastleObjectIntegrationService.CastleObjectType.Defense,
                _ => CastleObjectIntegrationService.CastleObjectType.Decoration
            };
        }

        private static string GetCastlePrefab(CastleObjectIntegrationService.CastleObjectType type)
        {
            return type switch
            {
                CastleObjectIntegrationService.CastleObjectType.Workbench => "decoration_torch", // Placeholder
                CastleObjectIntegrationService.CastleObjectType.Forge => "decoration_torch", // Placeholder
                CastleObjectIntegrationService.CastleObjectType.AlchemyTable => "decoration_torch", // Placeholder
                CastleObjectIntegrationService.CastleObjectType.Throne => "decoration_torch", // Placeholder
                CastleObjectIntegrationService.CastleObjectType.Storage => "decoration_torch", // Placeholder
                CastleObjectIntegrationService.CastleObjectType.Decoration => "decoration_torch",
                CastleObjectIntegrationService.CastleObjectType.Trap => "trigger_plate",
                CastleObjectIntegrationService.CastleObjectType.Utility => "trigger_proximity",
                CastleObjectIntegrationService.CastleObjectType.Defense => "wall_basic",
                _ => "decoration_torch"
            };
        }

        private static string GetFloorPrefab(CastleObjectIntegrationService.CastleObjectType type)
        {
            return type switch
            {
                CastleObjectIntegrationService.CastleObjectType.Storage => "floor_stone",
                CastleObjectIntegrationService.CastleObjectType.Workbench => "floor_wood",
                CastleObjectIntegrationService.CastleObjectType.Forge => "floor_stone",
                CastleObjectIntegrationService.CastleObjectType.AlchemyTable => "floor_tile",
                CastleObjectIntegrationService.CastleObjectType.Throne => "floor_marble",
                CastleObjectIntegrationService.CastleObjectType.Defense => "floor_stone",
                _ => "floor_wood" // Default wood floor
            };
        }

        private static string GetWallPrefab(CastleObjectIntegrationService.CastleObjectType type)
        {
            return type switch
            {
                CastleObjectIntegrationService.CastleObjectType.Defense => "wall_stone",
                CastleObjectIntegrationService.CastleObjectType.Storage => "wall_wood",
                CastleObjectIntegrationService.CastleObjectType.Workbench => "wall_wood",
                CastleObjectIntegrationService.CastleObjectType.Forge => "wall_stone",
                CastleObjectIntegrationService.CastleObjectType.AlchemyTable => "wall_tile",
                CastleObjectIntegrationService.CastleObjectType.Throne => "wall_marble",
                _ => "wall_wood" // Default wood wall
            };
        }

        private static string GetPillarPrefab(CastleObjectIntegrationService.CastleObjectType type)
        {
            return type switch
            {
                CastleObjectIntegrationService.CastleObjectType.Defense => "pillar_stone",
                CastleObjectIntegrationService.CastleObjectType.Storage => "pillar_wood",
                CastleObjectIntegrationService.CastleObjectType.Workbench => "pillar_wood",
                CastleObjectIntegrationService.CastleObjectType.Forge => "pillar_stone",
                CastleObjectIntegrationService.CastleObjectType.AlchemyTable => "pillar_tile",
                CastleObjectIntegrationService.CastleObjectType.Throne => "pillar_marble",
                _ => "pillar_wood" // Default wood pillar
            };
        }

        private static Entity BuildWallSegment(string wallPrefab, float3 basePosition, float height)
        {
            try
            {
                // Build wall segment with multiple layers for height
                var wallBase = WorldSpawnService.SpawnStructure(wallPrefab, basePosition);
                
                // Add wall layers for height (3 layers for 3m height)
                for (int h = 1; h <= height; h++)
                {
                    var wallLayerPos = basePosition + new float3(0, h, 0);
                    var wallLayer = WorldSpawnService.SpawnStructure(wallPrefab, wallLayerPos);
                    if (wallLayer == Entity.Null)
                    {
                        Plugin.Logger?.LogWarning($"Failed to spawn wall layer at height {h}");
                    }
                }
                
                return wallBase;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error building wall segment: {ex.Message}");
                return Entity.Null;
            }
        }

        private static Entity SpawnCastleObject(CastleObjectIntegrationService.CastleObjectType type, string name, float3 position)
        {
            var prefabName = GetCastlePrefab(type);
            if (string.IsNullOrEmpty(prefabName))
                return Entity.Null;

            var entity = WorldSpawnService.SpawnStructure(prefabName, position);
            return entity;
        }
        #endregion
    }
}
