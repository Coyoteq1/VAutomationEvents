using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using VAuto.Automation;
using VAuto.Data;
using VAuto.Services.Interfaces;
using VAuto.EventAdapters;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Handles swap flow separate from zone entry. Swap sessions can be started by boss events or commands.
    /// </summary>
    public class GearSwapSystem
    {
        private readonly IGearService _gear;
        private readonly ILogger<GearSwapSystem> _logger;

        public GearSwapSystem(IGearService gear, ILogger<GearSwapSystem> logger)
        {
            _gear = gear;
            _logger = logger;

            // Subscribe to boss events (replace with your event bus)
            BossEventBus.BossSpawned += OnBossSpawned;
            BossEventBus.BossDefeated += OnBossDefeated;

            // Optionally subscribe to explicit swap events
            SwapEventBus.SwapRequested += OnSwapRequested;
            SwapEventBus.SwapEnded += OnSwapEnded;
        }

        private void OnBossSpawned(string playerId, Zone zone, BossEntry boss)
        {
            if (boss?.BossGear == null || boss.BossGear.Count == 0) return;

            // Only swap if player opted in for auto-swap (you can add a separate toggle if desired)
            if (!_gear.IsAutoEquipEnabledForPlayer(playerId)) return;

            var sessionId = $"boss-{boss.BossName}-{Guid.NewGuid():N}";
            _gear.StartSwapSession(playerId, sessionId, boss.BossGear);
            _logger.LogInformation("Started boss swap session {Session} for player {Player} (boss {Boss})", sessionId, playerId, boss.BossName);
        }

        private void OnBossDefeated(string playerId, Zone zone, BossEntry boss)
        {
            // End all boss sessions for this boss (simple approach)
            var sessions = _gear.GetActiveSwapSessions(playerId);
            foreach (var s in sessions.Where(k => k.StartsWith("boss-")))
            {
                _gear.EndSwapSession(playerId, s);
                _logger.LogInformation("Ended boss swap session {Session} for player {Player}", s, playerId);
            }
        }

        private void OnSwapRequested(string playerId, string sessionId, IEnumerable<GearEntry> gear)
        {
            _gear.StartSwapSession(playerId, sessionId, gear);
            _logger.LogInformation("Manual swap session {Session} started for player {Player}", sessionId, playerId);
        }

        private void OnSwapEnded(string playerId, string sessionId)
        {
            _gear.EndSwapSession(playerId, sessionId);
            _logger.LogInformation("Manual swap session {Session} ended for player {Player}", sessionId, playerId);
        }
    }
}