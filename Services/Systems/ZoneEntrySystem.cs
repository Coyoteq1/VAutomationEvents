using Microsoft.Extensions.Logging;
using VAuto.Automation;
using VAuto.Data;
using VAuto.Services.Interfaces;
using VAuto.EventAdapters;

namespace VAuto.Services.Systems
{
    public class ZoneEntrySystem
    {
        private readonly IGearService _gear;
        private readonly ILogger<ZoneEntrySystem> _logger;

        public ZoneEntrySystem(IGearService gear, ILogger<ZoneEntrySystem> logger)
        {
            _gear = gear;
            _logger = logger;

            // Subscribe to zone events (replace with your event bus)
            ZoneEventBus.PlayerEnteredZone += OnPlayerEnteredZone;
            ZoneEventBus.PlayerLeftZone += OnPlayerLeftZone;
        }

        private void OnPlayerEnteredZone(string playerId, Zone zone)
        {
            // Enter-zone flow: only equip entry gear if player opted in and zone has entry gear
            if (zone?.Entry?.OnEnter == null) return;
            if (!zone.Entry.OnEnter.AutoEquipOnEnter) return; // zone-level toggle
            if (!_gear.IsAutoEquipEnabledForPlayer(playerId)) return; // player-level toggle

            var gearList = zone.Entry.OnEnter.GearList;
            if (gearList != null && gearList.Count > 0)
            {
                _gear.EquipGearForPlayer(playerId, gearList);
                _logger.LogDebug("Auto-equipped zone entry gear for player {Player} in zone {Zone}", playerId, zone.ZoneId);
            }
        }

        private void OnPlayerLeftZone(string playerId, Zone zone)
        {
            // Revert only if zone requested revert on exit and no active swap sessions exist
            if (zone?.Entry?.OnEnter?.RevertOnExit == true)
            {
                // Revert will handle swap sessions first; if swap sessions exist they will be ended first
                _gear.RevertPlayerGear(playerId);
                _logger.LogDebug("Reverted gear for player {Player} on leaving zone {Zone}", playerId, zone.ZoneId);
            }
        }
    }
}