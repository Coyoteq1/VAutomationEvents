using System;
using System.Collections.Generic;
using System.IO;
using VampireCommandFramework;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Data;
using VAuto.Services.Systems;
using VAuto.Services.World;
using VAuto.Utilities;
using System.Text.Json;
using Unity.Mathematics;

namespace VAuto.Commands.Zone
{
    /// <summary>
    /// Zone management commands - control arena zones.
    /// Uses ArenaZoneService for zone operations.
    /// </summary>
    public static class ZoneCommands
    {
        [Command("zoneactivate", description: "Activate an arena zone with the specified ID", adminOnly: true)]
        public static void ActivateZoneCommand(ChatCommandContext ctx, int arenaId = 0)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                
                // Create default zone if none exists
                var zone = new VAuto.Data.ArenaZone
                {
                    Center = new float3(-1000f, 5f, -500f),
                    Radius = 50f,
                    Shape = VAuto.Data.ArenaZoneShape.Circle
                };

                if (zoneService.ActivateZone(arenaId, zone))
                {
                    ctx.Reply($"Arena zone {arenaId} activated successfully!");
                    ctx.Reply($"Center: {zone.Center}, Radius: {zone.Radius}m");
                }
                else
                {
                    ctx.Reply("Failed to activate arena zone.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ActivateZoneCommand: {ex.Message}");
                ctx.Reply("An error occurred while activating the zone.");
            }
        }

        [Command("zonedeactivate", description: "Deactivate an arena zone with the specified ID", adminOnly: true)]
        public static void DeactivateZoneCommand(ChatCommandContext ctx, int arenaId = 0)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;

                if (zoneService.DeactivateZone(arenaId))
                {
                    ctx.Reply($"Arena zone {arenaId} deactivated successfully!");
                }
                else
                {
                    ctx.Reply("Failed to deactivate arena zone.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in DeactivateZoneCommand: {ex.Message}");
                ctx.Reply("An error occurred while deactivating the zone.");
            }
        }

        [Command("zonestatus", description: "Show the current status of all active arena zones including player counts and zone information", adminOnly: true)]
        public static void ZoneStatusCommand(ChatCommandContext ctx)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                var activeZones = zoneService.GetActiveArenaIds();

                ctx.Reply("Zone Status:");
                ctx.Reply($"  Active Zones: {activeZones.Count}");

                foreach (var arenaId in activeZones)
                {
                    var zoneState = zoneService.GetZoneState(arenaId);
                    if (zoneState != null)
                    {
                        ctx.Reply($"  Arena {arenaId}:");
                        ctx.Reply($"    Active Players: {zoneState.ActivePlayers.Count}");
                        ctx.Reply($"    Center: {zoneState.Zone.Center}");
                        ctx.Reply($"    Radius: {zoneState.Zone.Radius}m");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ZoneStatusCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting zone status.");
            }
        }

        [Command("zoneinfo", description: "Display information about the arena zone at your current location", adminOnly: true)]
        public static void ZoneInfoCommand(ChatCommandContext ctx)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                var position = VRCore.EM.GetComponentData<Translation>(character).Value;
                var zoneService = ArenaZoneService.Instance;

                ctx.Reply("Zone Information:");
                ctx.Reply($"  Your Position: {position}");

                var zone = zoneService.GetZoneContainingPosition(position);
                if (zone != null)
                {
                    ctx.Reply($"  In Arena Zone: {zone.ArenaId}");
                    ctx.Reply($"  Zone Center: {zone.Zone.Center}");
                    ctx.Reply($"  Zone Radius: {zone.Zone.Radius}m");
                    ctx.Reply($"  Active Players: {zone.ActivePlayers.Count}");
                }
                else
                {
                    ctx.Reply("  Not in any arena zone");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ZoneInfoCommand: {ex.Message}");
                ctx.Reply("An error occurred while getting zone information.");
            }
        }

        [Command("zoneclear", description: "Deactivate all active arena zones", adminOnly: true)]
        public static void ClearZonesCommand(ChatCommandContext ctx)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                zoneService.ClearAll();

                ctx.Reply("All arena zones cleared!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ClearZonesCommand: {ex.Message}");
                ctx.Reply("An error occurred while clearing zones.");
            }
        }

        [Command("zonecreate", description: "Create a new arena zone with specified name, shape, and coordinates", adminOnly: true)]
        public static void CreateZoneCommand(ChatCommandContext ctx, string name, string shape, float centerX, float centerY, float centerZ, float radius = 50f, float width = 0f, float length = 0f)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                
                // Parse shape
                if (!Enum.TryParse<ArenaZoneShape>(shape, true, out var zoneShape))
                {
                    ctx.Reply($"Invalid shape: {shape}. Available: Circle, Box");
                    return;
                }

                var zone = new ArenaZone
                {
                    Name = name,
                    Center = new float3(centerX, centerY, centerZ),
                    Shape = zoneShape,
                    Radius = zoneShape == ArenaZoneShape.Circle ? radius : 0f,
                    Dimensions = zoneShape == ArenaZoneShape.Box ? new float2(width, length) : float2.zero
                };

                // Use next available arena ID
                var arenaId = DateTime.UtcNow.Second % 10; // Simple ID generation
                
                if (zoneService.ActivateZone(arenaId, zone))
                {
                    // Save zone configuration using JsonUtil
                    var zonesPath = Path.Combine("config", "VAuto.Arena", "zones.json");
                    var zonesData = new { Zones = new[] { zone }, CreatedAt = DateTime.UtcNow };
                    JsonUtil.SaveToFile(zonesData, zonesPath, $"Arena zones configuration\nLast updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                    
                    ctx.Reply($"Zone '{name}' created successfully with ID {arenaId}!");
                    ctx.Reply($"Shape: {zoneShape}, Center: {zone.Center}, {(zoneShape == ArenaZoneShape.Circle ? $"Radius: {zone.Radius}m" : $"Size: {zone.Dimensions.x}x{zone.Dimensions.y}")}");
                }
                else
                {
                    ctx.Reply("Failed to create zone.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CreateZoneCommand: {ex.Message}");
                ctx.Reply("An error occurred while creating the zone.");
            }
        }

        [Command("zonesave", description: "Save current active arena zones configuration to a JSON file", adminOnly: true)]
        public static void SaveZonesCommand(ChatCommandContext ctx, string fileName = "zones")
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                var activeArenaIds = zoneService.GetActiveArenaIds();

                var fileNameWithExt = fileName.EndsWith(".json") ? fileName : $"{fileName}.json";
                var filePath = Path.Combine(Plugin.ConfigPath, "zones", fileNameWithExt);

                // Ensure the zones directory exists
                var zonesDir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(zonesDir))
                {
                    Directory.CreateDirectory(zonesDir);
                }

                var commentHeader = $"Arena zones configuration - {activeArenaIds.Count} active zones\n" +
                                   $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                                   $"Exported by: {ctx.Event.User.CharacterName}";

                // Create zones data structure with full zone information
                var zonesData = new {
                    Zones = activeArenaIds.Select(id => {
                        var zoneState = zoneService.GetZoneState(id);
                        return zoneState != null ? new {
                            ArenaId = id,
                            Zone = new {
                                Name = zoneState.Zone.Name,
                                Center = zoneState.Zone.Center,
                                Radius = zoneState.Zone.Radius,
                                Shape = zoneState.Zone.Shape.ToString(),
                                Dimensions = zoneState.Zone.Dimensions
                            },
                            IsActive = true,
                            ActivePlayers = zoneState.ActivePlayers.Count,
                            CreatedAt = zoneState.CreatedAt
                        } : null;
                    }).Where(z => z != null).ToArray(),
                    CreatedAt = DateTime.UtcNow,
                    ExportedBy = ctx.Event.User.CharacterName,
                    TotalActiveZones = activeArenaIds.Count
                };

                if (JsonUtil.SaveToFile(zonesData, filePath, commentHeader))
                {
                    ctx.Reply($"Saved {activeArenaIds.Count} active zones to {filePath}");
                    ctx.Reply($"File location: {Path.GetFullPath(filePath)}");
                }
                else
                {
                    ctx.Reply("Failed to save zones configuration.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SaveZonesCommand: {ex.Message}");
                ctx.Reply($"An error occurred while saving zones: {ex.Message}");
            }
        }

        [Command("zoneload", description: "Load arena zones configuration from a JSON file", adminOnly: true)]
        public static void LoadZonesCommand(ChatCommandContext ctx, string fileName)
        {
            try
            {
                var fileNameWithExt = fileName.EndsWith(".json") ? fileName : $"{fileName}.json";
                var filePath = Path.Combine(Plugin.ConfigPath, "zones", fileNameWithExt);

                if (!File.Exists(filePath))
                {
                    ctx.Reply($"File not found: {filePath}");
                    ctx.Reply($"Full path: {Path.GetFullPath(filePath)}");
                    return;
                }

                var zonesData = JsonUtil.LoadFromFile<dynamic>(filePath);

                if (zonesData != null)
                {
                    ctx.Reply($"Zone configuration loaded from {filePath}");
                    ctx.Reply($"Found {zonesData.Zones?.Length ?? 0} zones in configuration");
                    ctx.Reply("Note: Individual zone activation requires manual setup using .zoneactivate");
                }
                else
                {
                    ctx.Reply("No zones found in file or failed to load.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in LoadZonesCommand: {ex.Message}");
                ctx.Reply($"An error occurred while loading zones: {ex.Message}");
            }
        }

        [Command("zonelist", description: "Display a list of all active arena zones with their details", adminOnly: true)]
        public static void ListZonesCommand(ChatCommandContext ctx)
        {
            try
            {
                var zoneService = ArenaZoneService.Instance;
                var activeArenaIds = zoneService.GetActiveArenaIds();
                
                if (activeArenaIds.Count == 0)
                {
                    ctx.Reply("No active zones configured.");
                    return;
                }

                ctx.Reply($"=== Active Arena Zones ({activeArenaIds.Count}) ===");
                
                foreach (var arenaId in activeArenaIds)
                {
                    var zoneState = zoneService.GetZoneState(arenaId);
                    if (zoneState != null)
                    {
                        var zone = zoneState.Zone;
                        var shapeInfo = zone.Shape == ArenaZoneShape.Circle 
                            ? $"Circle (R: {zone.Radius}m)" 
                            : $"Box ({zone.Dimensions.x}x{zone.Dimensions.y})";
                        
                        ctx.Reply($"• Arena {arenaId}: {zone.Name} - {shapeInfo}");
                        ctx.Reply($"  Center: {zone.Center}");
                        ctx.Reply($"  Players: {zoneState.ActivePlayers.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListZonesCommand: {ex.Message}");
                ctx.Reply("An error occurred while listing zones.");
            }
        }
    }
}
