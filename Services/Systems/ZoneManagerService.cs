using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VAuto.Data;
using VAuto.Services.Systems;

namespace VAuto.Services.Systems
{
    public class ZoneManagerService
    {
        private readonly ZoneValidatorService _zoneValidator;
        private readonly AutomationExecutionEngine _executionEngine;
        private readonly AutomationGateService _automationGate;
        private readonly PrefabResolverService _prefabResolver;
        private readonly CastleRegistryService _castleRegistry;
        private readonly LogisticsAutomationService _logisticsService;

        public ZoneManagerService(
            ZoneValidatorService zoneValidator,
            AutomationExecutionEngine executionEngine,
            AutomationGateService automationGate,
            PrefabResolverService prefabResolver,
            CastleRegistryService castleRegistry,
            LogisticsAutomationService logisticsService)
        {
            _zoneValidator = zoneValidator;
            _executionEngine = executionEngine;
            _automationGate = automationGate;
            _prefabResolver = prefabResolver;
            _castleRegistry = castleRegistry;
            _logisticsService = logisticsService;
        }

        public ZoneCreateResult CreateZone(Zone zone, AutomationContext context)
        {
            // Validate the zone first
            var validationResult = _zoneValidator.ValidateZone(zone, context);
            if (!validationResult.IsValid)
            {
                return new ZoneCreateResult
                {
                    Success = false,
                    Messages = validationResult.Messages,
                    RequiresDevApproval = validationResult.RequiresDevApproval,
                    RequiresSnapshot = validationResult.RequiresSnapshot
                };
            }

            // Check if zone already exists
            if (context.Zones != null && context.Zones.Any(z => z.ZoneId == zone.ZoneId))
            {
                return new ZoneCreateResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zone.ZoneId} already exists" }
                };
            }

            // Add zone to context
            if (context.Zones == null)
            {
                context.Zones = new List<Zone>();
            }
            context.Zones.Add(zone);

            // Check automation gates
            var gateResult = _automationGate.CheckZoneGates(zone, context);
            if (!gateResult.Passed)
            {
                return new ZoneCreateResult
                {
                    Success = false,
                    Messages = gateResult.Messages,
                    RequiresDevApproval = gateResult.RequiresDevApproval,
                    RequiresSnapshot = gateResult.RequiresSnapshot
                };
            }

            return new ZoneCreateResult
            {
                Success = true,
                Messages = new List<string> { $"Zone {zone.Name} created successfully" },
                ZoneId = zone.ZoneId
            };
        }

        public ZoneUpdateResult UpdateZone(string zoneId, Zone updatedZone, AutomationContext context)
        {
            // Find existing zone
            var existingZone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (existingZone == null)
            {
                return new ZoneUpdateResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            // Validate the updated zone
            var validationResult = _zoneValidator.ValidateZone(updatedZone, context);
            if (!validationResult.IsValid)
            {
                return new ZoneUpdateResult
                {
                    Success = false,
                    Messages = validationResult.Messages,
                    RequiresDevApproval = validationResult.RequiresDevApproval,
                    RequiresSnapshot = validationResult.RequiresSnapshot
                };
            }

            // Check if zone ID is being changed to an existing one
            if (updatedZone.ZoneId != zoneId && context.Zones.Any(z => z.ZoneId == updatedZone.ZoneId))
            {
                return new ZoneUpdateResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {updatedZone.ZoneId} already exists" }
                };
            }

            // Remove old zone and add updated one
            context.Zones.Remove(existingZone);
            context.Zones.Add(updatedZone);

            // Check automation gates
            var gateResult = _automationGate.CheckZoneGates(updatedZone, context);
            if (!gateResult.Passed)
            {
                return new ZoneUpdateResult
                {
                    Success = false,
                    Messages = gateResult.Messages,
                    RequiresDevApproval = gateResult.RequiresDevApproval,
                    RequiresSnapshot = gateResult.RequiresSnapshot
                };
            }

            return new ZoneUpdateResult
            {
                Success = true,
                Messages = new List<string> { $"Zone {updatedZone.Name} updated successfully" },
                ZoneId = updatedZone.ZoneId
            };
        }

        public ZoneDeleteResult DeleteZone(string zoneId, AutomationContext context)
        {
            // Find existing zone
            var existingZone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (existingZone == null)
            {
                return new ZoneDeleteResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            // Check if zone can be deleted (e.g., no active players, no dependencies)
            var canDeleteResult = CanDeleteZone(existingZone, context);
            if (!canDeleteResult.CanDelete)
            {
                return new ZoneDeleteResult
                {
                    Success = false,
                    Messages = canDeleteResult.Messages
                };
            }

            // Remove zone from context
            context.Zones.Remove(existingZone);

            return new ZoneDeleteResult
            {
                Success = true,
                Messages = new List<string> { $"Zone {existingZone.Name} deleted successfully" },
                ZoneId = existingZone.ZoneId
            };
        }

        private CanDeleteZoneResult CanDeleteZone(Zone zone, AutomationContext context)
        {
            var result = new CanDeleteZoneResult { CanDelete = true, Messages = new List<string>() };

            // Check if zone has active players (simplified - in real implementation, check player positions)
            // This would integrate with your player tracking system
            // if (PlayerService.HasPlayersInZone(zone.ZoneId))
            // {
            //     result.CanDelete = false;
            //     result.Messages.Add($"Cannot delete zone {zone.Name} - players are currently in this zone");
            // }

            // Check if zone is referenced by other systems
            if (context.Castles != null && context.Castles.Any(c => c.ZoneId == zone.ZoneId))
            {
                result.CanDelete = false;
                result.Messages.Add($"Cannot delete zone {zone.Name} - it is referenced by castles");
            }

            return result;
        }

        public ZoneGetResult GetZone(string zoneId, AutomationContext context)
        {
            var zone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (zone == null)
            {
                return new ZoneGetResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            return new ZoneGetResult
            {
                Success = true,
                Zone = zone
            };
        }

        public ZoneListResult ListZones(AutomationContext context)
        {
            if (context.Zones == null || context.Zones.Count == 0)
            {
                return new ZoneListResult
                {
                    Success = true,
                    Zones = new List<Zone>(),
                    Messages = new List<string> { "No zones found" }
                };
            }

            return new ZoneListResult
            {
                Success = true,
                Zones = context.Zones,
                Messages = new List<string> { $"{context.Zones.Count} zones found" }
            };
        }

        public ZoneEnterResult EnterZone(string zoneId, string playerId, AutomationContext context)
        {
            var zone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (zone == null)
            {
                return new ZoneEnterResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            // Check if player is already in a zone
            // This would integrate with your player state tracking
            // if (PlayerService.IsPlayerInZone(playerId))
            // {
            //     return new ZoneEnterResult
            //     {
            //         Success = false,
            //         Messages = new List<string> { $"Player is already in a zone" }
            //     };
            // }

            // Apply zone entry effects
            var entryResult = ApplyZoneEntryEffects(zone, playerId, context);
            if (!entryResult.Success)
            {
                return new ZoneEnterResult
                {
                    Success = false,
                    Messages = entryResult.Messages
                };
            }

            // Mark player as entered zone (simplified - would integrate with player state)
            // PlayerService.MarkPlayerEnteredZone(playerId, zoneId);

            return new ZoneEnterResult
            {
                Success = true,
                Messages = new List<string> { $"Player {playerId} entered zone {zone.Name}" },
                ZoneId = zone.ZoneId,
                ZoneName = zone.Name
            };
        }

        public ZoneExitResult ExitZone(string zoneId, string playerId, AutomationContext context)
        {
            var zone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (zone == null)
            {
                return new ZoneExitResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            // Check if player is actually in this zone
            // This would integrate with your player state tracking
            // if (!PlayerService.IsPlayerInZone(playerId, zoneId))
            // {
            //     return new ZoneExitResult
            //     {
            //         Success = false,
            //         Messages = new List<string> { $"Player is not in zone {zone.Name}" }
            //     };
            // }

            // Apply zone exit effects
            var exitResult = ApplyZoneExitEffects(zone, playerId, context);
            if (!exitResult.Success)
            {
                return new ZoneExitResult
                {
                    Success = false,
                    Messages = exitResult.Messages
                };
            }

            // Mark player as exited zone (simplified - would integrate with player state)
            // PlayerService.MarkPlayerExitedZone(playerId, zoneId);

            return new ZoneExitResult
            {
                Success = true,
                Messages = new List<string> { $"Player {playerId} exited zone {zone.Name}" },
                ZoneId = zone.ZoneId,
                ZoneName = zone.Name
            };
        }

        private ZoneEffectResult ApplyZoneEntryEffects(Zone zone, string playerId, AutomationContext context)
        {
            var result = new ZoneEffectResult { Success = true, Messages = new List<string>() };

            try
            {
                // Apply gear
                if (zone.Entry?.OnEnter?.GiveGear == true && zone.Entry.OnEnter.GearList != null)
                {
                    foreach (var gear in zone.Entry.OnEnter.GearList)
                    {
                        // This would integrate with your inventory system
                        // InventoryService.AddItem(playerId, gear.ItemName, gear.Count, gear.Equip);
                        result.Messages.Add($"Added {gear.Count}x {gear.ItemName} to player {playerId}");
                    }
                }

                // Apply blood type override
                if (zone.Entry?.OnEnter?.OverrideBlood == true && !string.IsNullOrEmpty(zone.Entry.OnEnter.BloodType))
                {
                    // This would integrate with your blood type system
                    // BloodTypeService.SetPlayerBloodType(playerId, zone.Entry.OnEnter.BloodType);
                    result.Messages.Add($"Set blood type to {zone.Entry.OnEnter.BloodType} for player {playerId}");
                }

                // Apply UI effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnEnter?.UiEffect))
                {
                    // This would integrate with your UI system
                    // UiService.ApplyEffect(playerId, zone.Entry.OnEnter.UiEffect);
                    result.Messages.Add($"Applied UI effect {zone.Entry.OnEnter.UiEffect} to player {playerId}");
                }

                // Apply glow effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnEnter?.GlowEffect))
                {
                    // This would integrate with your glow effect system
                    // GlowService.ApplyEffect(playerId, zone.Entry.OnEnter.GlowEffect);
                    result.Messages.Add($"Applied glow effect {zone.Entry.OnEnter.GlowEffect} to player {playerId}");
                }

                // Apply map effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnEnter?.MapEffect))
                {
                    // This would integrate with your map system
                    // MapService.ApplyEffect(playerId, zone.Entry.OnEnter.MapEffect);
                    result.Messages.Add($"Applied map effect {zone.Entry.OnEnter.MapEffect} to player {playerId}");
                }

                // Show entry message
                if (!string.IsNullOrEmpty(zone.Entry?.OnEnter?.Message))
                {
                    // This would integrate with your chat/message system
                    // ChatService.SendMessage(playerId, zone.Entry.OnEnter.Message);
                    result.Messages.Add($"Sent message to player {playerId}: {zone.Entry.OnEnter.Message}");
                }

                // Spawn mobs
                if (zone.Mobs != null && zone.Mobs.Count > 0)
                {
                    foreach (var mob in zone.Mobs)
                    {
                        // This would integrate with your mob spawning system
                        // MobService.SpawnMobs(zone.ZoneId, mob.MobName, mob.Count);
                        result.Messages.Add($"Spawned {mob.Count}x {mob.MobName} in zone {zone.Name}");
                    }
                }

                // Spawn boss if applicable
                if (zone.Boss != null && !string.IsNullOrEmpty(zone.Boss.BossName))
                {
                    // This would integrate with your boss spawning system
                    // BossService.SpawnBoss(zone.ZoneId, zone.Boss.BossName, zone.Boss.BossLevel);
                    result.Messages.Add($"Spawned boss {zone.Boss.BossName} (level {zone.Boss.BossLevel}) in zone {zone.Name}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error applying zone entry effects: {ex.Message}");
            }

            return result;
        }

        private ZoneEffectResult ApplyZoneExitEffects(Zone zone, string playerId, AutomationContext context)
        {
            var result = new ZoneEffectResult { Success = true, Messages = new List<string>() };

            try
            {
                // Remove gear if configured
                if (zone.Entry?.OnExit?.RemoveGear == true)
                {
                    // This would integrate with your inventory system
                    // InventoryService.ClearZoneGear(playerId);
                    result.Messages.Add($"Removed zone gear from player {playerId}");
                }

                // Restore blood type if configured
                if (zone.Entry?.OnExit?.RestoreBlood == true)
                {
                    // This would integrate with your blood type system
                    // BloodTypeService.RestorePlayerBloodType(playerId);
                    result.Messages.Add($"Restored original blood type for player {playerId}");
                }

                // Apply exit UI effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnExit?.UiEffect))
                {
                    // This would integrate with your UI system
                    // UiService.ApplyEffect(playerId, zone.Entry.OnExit.UiEffect);
                    result.Messages.Add($"Applied exit UI effect {zone.Entry.OnExit.UiEffect} to player {playerId}");
                }

                // Apply exit glow effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnExit?.GlowEffect))
                {
                    // This would integrate with your glow effect system
                    // GlowService.ApplyEffect(playerId, zone.Entry.OnExit.GlowEffect);
                    result.Messages.Add($"Applied exit glow effect {zone.Entry.OnExit.GlowEffect} to player {playerId}");
                }

                // Apply exit map effects
                if (!string.IsNullOrEmpty(zone.Entry?.OnExit?.MapEffect))
                {
                    // This would integrate with your map system
                    // MapService.ApplyEffect(playerId, zone.Entry.OnExit.MapEffect);
                    result.Messages.Add($"Applied exit map effect {zone.Entry.OnExit.MapEffect} to player {playerId}");
                }

                // Show exit message
                if (!string.IsNullOrEmpty(zone.Entry?.OnExit?.Message))
                {
                    // This would integrate with your chat/message system
                    // ChatService.SendMessage(playerId, zone.Entry.OnExit.Message);
                    result.Messages.Add($"Sent exit message to player {playerId}: {zone.Entry.OnExit.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"Error applying zone exit effects: {ex.Message}");
            }

            return result;
        }

        public ZoneSaveResult SaveZoneSchematic(string zoneId, string schematicId, AutomationContext context)
        {
            var zone = context.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            if (zone == null)
            {
                return new ZoneSaveResult
                {
                    Success = false,
                    Messages = new List<string> { $"Zone with ID {zoneId} not found" }
                };
            }

            // Update zone schematic
            if (zone.Schematic == null)
            {
                zone.Schematic = new Schematic();
            }
            zone.Schematic.SchematicId = schematicId;
            zone.Schematic.Enabled = true;

            // This would integrate with your schematic system
            // SchematicService.SaveZoneSchematic(zoneId, schematicId);

            return new ZoneSaveResult
            {
                Success = true,
                Messages = new List<string> { $"Saved schematic {schematicId} for zone {zone.Name}" },
                ZoneId = zone.ZoneId,
                SchematicId = schematicId
            };
        }
    }

    // Result classes for zone operations
    public class ZoneCreateResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }

    public class ZoneUpdateResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
        public bool RequiresDevApproval { get; set; }
        public bool RequiresSnapshot { get; set; }
    }

    public class ZoneDeleteResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
    }

    public class ZoneGetResult
    {
        public bool Success { get; set; }
        public Zone Zone { get; set; }
        public List<string> Messages { get; set; }
    }

    public class ZoneListResult
    {
        public bool Success { get; set; }
        public List<Zone> Zones { get; set; }
        public List<string> Messages { get; set; }
    }

    public class ZoneEnterResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
        public string ZoneName { get; set; }
    }

    public class ZoneExitResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
        public string ZoneName { get; set; }
    }

    public class ZoneEffectResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
    }

    public class ZoneSaveResult
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public string ZoneId { get; set; }
        public string SchematicId { get; set; }
    }

    public class CanDeleteZoneResult
    {
        public bool CanDelete { get; set; }
        public List<string> Messages { get; set; }
    }
}