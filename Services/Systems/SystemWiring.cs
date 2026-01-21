using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services.Interfaces;
using VAuto.Services.Systems;
using VAuto.Data;
using VAuto.EventAdapters;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// System Wiring - Connects all services and systems together
    /// Initializes the complete VAuto ecosystem
    /// </summary>
    public static class SystemWiring
    {
        private static bool _isWired;
        private static readonly object _lock = new object();
        private static ManualLogSource Log => Plugin.Logger;

        /// <summary>
        /// Wire up all systems in proper dependency order
        /// </summary>
        public static void WireUpSystems()
        {
            lock (_lock)
            {
                if (_isWired) return;
                
                try
                {
                    Log?.LogInfo("[SystemWiring] Starting system wiring...");

                    // Phase 1: Core Infrastructure
                    WireCoreInfrastructure();

                    // Phase 2: Service Layer
                    WireServiceLayer();

                    // Phase 3: Command Layer
                    WireCommandLayer();

                    // Phase 4: UI Layer
                    WireUILayer();

                    // Phase 5: Event Integration
                    WireEventIntegration();

                    // Apply Harmony patches for game system modifications
                    ApplyHarmonyPatches();

                    _isWired = true;
                    Log?.LogInfo("[SystemWiring] All systems wired successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[SystemWiring] Failed to wire systems: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Wire core infrastructure services
        /// </summary>
        private static void WireCoreInfrastructure()
        {
            Log?.LogInfo("[SystemWiring] Wiring core infrastructure...");

            // Initialize core services first
            ServiceManager.RegisterService<VRCore>(VRCore.Instance);
            ServiceManager.RegisterService<ZoneService>(ZoneService.Instance);
            ServiceManager.RegisterService<SnapshotService>(SnapshotService.Instance);
            ServiceManager.RegisterService<ZoneManagerService>(ZoneManagerService.Instance);
            ServiceManager.RegisterService<AILearningService>(AILearningService.Instance);
            ServiceManager.RegisterService<ZoneValidatorService>(ZoneValidatorService.Instance);

            // Initialize JSON utilities
            JsonUtil.Initialize();
            SnapshotUuidGenerator.Initialize();
            ArenaUnlockService.ResetAllTracking();

            Log?.LogInfo("[SystemWiring] Core infrastructure wired");
        }

        /// <summary>
        /// Wire service layer
        /// </summary>
        private static void WireServiceLayer()
        {
            Log?.LogInfo("[SystemWiring] Wiring service layer...");

            // Register all services
            ServiceManager.RegisterService<PlayerService>(PlayerService.Instance);
            ServiceManager.RegisterService<GearService>(GearService.Instance);
            ServiceManager.RegisterService<CastleRegistryService>(CastleRegistryService.Instance);
            ServiceManager.RegisterService<GlobalMapIconService>(GlobalMapIconService.Instance);
            ServiceManager.RegisterService<LogisticsAutomationService>(LogisticsAutomationService.Instance);
            ServiceManager.RegisterService<AbilityOverrideService>(AbilityOverrideService.Instance);
            ServiceManager.RegisterService<EnhancedArenaSnapshotService>(EnhancedArenaSnapshotService.Instance);
            ServiceManager.RegisterService<ArenaGlowService>(ArenaGlowService.Instance);
            ServiceManager.RegisterService<ArenaBuildService>(ArenaBuildService.Instance);
            ServiceManager.RegisterService<AutoEnterService>(AutoEnterService.Instance);
            ServiceManager.RegisterService<RespawnPreventionService>(RespawnPreventionService.Instance);
            ServiceManager.RegisterService<NameTagService>(NameTagService.Instance);
            ServiceManager.RegisterService<MapIconService>(MapIconService.Instance);
            ServiceManager.RegisterService<LocalizationService>(LocalizationService.Instance);
            ServiceManager.RegisterService<ArenaObjectService>(ArenaObjectService.Instance);
            ServiceManager.RegisterService<ArenaDataSaver>(ArenaDataSaver.Instance);
            ServiceManager.RegisterService<AutoComponentSaver>(AutoComponentSaver.Instance);

            Log?.LogInfo("[SystemWiring] Service layer wired");
        }

        /// <summary>
        /// Wire command layer
        /// </summary>
        private static void WireCommandLayer()
        {
            Log?.LogInfo("[SystemWiring] Wiring command layer...");

            // Commands are automatically discovered via attributes
            // Just ensure command framework is initialized
            VampireCommandFramework.CommandManager.Initialize();

            Log?.LogInfo("[SystemWiring] Command layer wired");
        }

        /// <summary>
        /// Wire UI layer
        /// </summary>
        private static void WireUILayer()
        {
            Log?.LogInfo("[SystemWiring] Wiring UI layer...");

            // Initialize UI systems
            MouseBuildingSystem.Instance.Initialize();
            ArenaUIManager.Instance.Initialize();
            UISettings.Instance.Initialize();

            Log?.LogInfo("[SystemWiring] UI layer wired");
        }

        /// <summary>
        /// Wire event integration
        /// </summary>
        private static void WireEventIntegration()
        {
            Log?.LogInfo("[SystemWiring] Wiring event integration...");

            // Set up event adapters
            ZoneEventBus.Initialize();
            GameEventBus.Initialize();
            ArenaEventBus.Initialize();

            // Wire cross-system events
            WireZoneEvents();
            WireArenaEvents();
            WireGameEvents();

            Log?.LogInfo("[SystemWiring] Event integration wired");
        }

        /// <summary>
        /// Wire zone-related events
        /// </summary>
        private static void WireZoneEvents()
        {
            // Zone entry/exit events
            ZoneEventBus.PlayerEnteredZone += OnPlayerEnteredZone;
            ZoneEventBus.PlayerLeftZone += OnPlayerLeftZone;

            Log?.LogInfo("[SystemWiring] Zone events wired");
        }

        /// <summary>
        /// Wire arena-related events
        /// </summary>
        private static void WireArenaEvents()
        {
            // Arena lifecycle events
            ArenaEventBus.PlayerEnteredArena += OnPlayerEnteredArena;
            ArenaEventBus.PlayerExitedArena += OnPlayerExitedArena;
            ArenaEventBus.ArenaStateChanged += OnArenaStateChanged;

            Log?.LogInfo("[SystemWiring] Arena events wired");
        }

        /// <summary>
        /// Wire game-related events
        /// </summary>
        private static void WireGameEvents()
        {
            // Game state events
            GameEventBus.GameStateChanged += OnGameStateChanged;
            GameEventBus.PlayerConnected += OnPlayerConnected;
            GameEventBus.PlayerDisconnected += OnPlayerDisconnected;

            Log?.LogInfo("[SystemWiring] Game events wired");
        }

        /// <summary>
        /// Event handlers for zone events
        /// </summary>
        private static void OnPlayerEnteredZone(string playerId, Zone zone)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerId} entered zone {zone.ZoneId}");

                // Trigger gear auto-equip if configured
                var gearService = ServiceManager.GetService<GearService>();
                if (gearService != null && gearService.IsAutoEquipEnabledForPlayer(playerId))
                {
                    // Handle zone-specific gear loading
                    Log?.LogDebug($"[SystemWiring] Auto-equip triggered for player {playerId}");
                }

                // Notify other systems
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerEnteredZone(playerId, zone);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerEnteredZone: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for zone exit events
        /// </summary>
        private static void OnPlayerLeftZone(string playerId, Zone zone)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerId} left zone {zone.ZoneId}");

                // Trigger gear revert if configured
                var gearService = ServiceManager.GetService<GearService>();
                if (gearService != null && gearService.IsAutoEquipEnabledForPlayer(playerId))
                {
                    // Handle gear reversion
                    Log?.LogDebug($"[SystemWiring] Gear revert triggered for player {playerId}");
                }

                // Notify other systems
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerLeftZone(playerId, zone);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerLeftZone: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for arena entry events
        /// </summary>
        private static void OnPlayerEnteredArena(string playerId)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerId} entered arena");

                // Notify arena systems
                var arenaSnapshotService = ServiceManager.GetService<EnhancedArenaSnapshotService>();
                arenaSnapshotService?.OnPlayerEnteredArena(playerId);

                var arenaGlowService = ServiceManager.GetService<ArenaGlowService>();
                arenaGlowService?.OnPlayerEnteredArena(playerId);

                // Notify player service
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerEnteredArena(playerId);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerEnteredArena: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for arena exit events
        /// </summary>
        private static void OnPlayerExitedArena(string playerId)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerId} exited arena");

                // Notify arena systems
                var arenaSnapshotService = ServiceManager.GetService<EnhancedArenaSnapshotService>();
                arenaSnapshotService?.OnPlayerExitedArena(playerId);

                var arenaGlowService = ServiceManager.GetService<ArenaGlowService>();
                arenaGlowService?.OnPlayerExitedArena(playerId);

                // Notify player service
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerExitedArena(playerId);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerExitedArena: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for arena state changes
        /// </summary>
        private static void OnArenaStateChanged(string arenaId, string state, Dictionary<string, object> data)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Arena {arenaId} state changed to {state}");

                // Notify all arena-interested services
                var arenaBuildService = ServiceManager.GetService<ArenaBuildService>();
                arenaBuildService?.OnArenaStateChanged(arenaId, state, data);

                var arenaObjectService = ServiceManager.GetService<ArenaObjectService>();
                arenaObjectService?.OnArenaStateChanged(arenaId, state, data);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnArenaStateChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for game state changes
        /// </summary>
        private static void OnGameStateChanged(string newState, Dictionary<string, object> data)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Game state changed to {newState}");

                // Notify all game-interested services
                var castleRegistry = ServiceManager.GetService<CastleRegistryService>();
                castleRegistry?.OnGameStateChanged(newState, data);

                var logisticsService = ServiceManager.GetService<LogisticsAutomationService>();
                logisticsService?.OnGameStateChanged(newState, data);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnGameStateChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for player connection
        /// </summary>
        private static void OnPlayerConnected(string playerId, string playerName)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerName} ({playerId}) connected");

                // Initialize player data
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerConnected(playerId, playerName);

                // Load player snapshots if any
                var snapshotService = ServiceManager.GetService<SnapshotService>();
                snapshotService?.LoadPlayerSnapshot(playerId);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerConnected: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handlers for player disconnection
        /// </summary>
        private static void OnPlayerDisconnected(string playerId)
        {
            try
            {
                Log?.LogDebug($"[SystemWiring] Player {playerId} disconnected");

                // Cleanup player data
                var playerService = ServiceManager.GetService<PlayerService>();
                playerService?.OnPlayerDisconnected(playerId);

                // Save player snapshot if needed
                var snapshotService = ServiceManager.GetService<SnapshotService>();
                snapshotService?.SavePlayerSnapshot(playerId);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Error in OnPlayerDisconnected: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if systems are wired
        /// </summary>
        public static bool IsWired => _isWired;

        /// <summary>
        /// Unwire all systems
        /// </summary>
        public static void UnwireSystems()
        {
            lock (_lock)
            {
                if (!_isWired) return;

                try
                {
                    Log?.LogInfo("[SystemWiring] Unwiring systems...");

                    // Cleanup event handlers
                    ZoneEventBus.PlayerEnteredZone -= OnPlayerEnteredZone;
                    ZoneEventBus.PlayerLeftZone -= OnPlayerLeftZone;
                    ArenaEventBus.PlayerEnteredArena -= OnPlayerEnteredArena;
                    ArenaEventBus.PlayerExitedArena -= OnPlayerExitedArena;
                    ArenaEventBus.ArenaStateChanged -= OnArenaStateChanged;
                    GameEventBus.GameStateChanged -= OnGameStateChanged;
                    GameEventBus.PlayerConnected -= OnPlayerConnected;
                    GameEventBus.PlayerDisconnected -= OnPlayerDisconnected;

                    // Cleanup services
                    ServiceManager.CleanupAllServices();

                    _isWired = false;
                    Log?.LogInfo("[SystemWiring] Systems unwired successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[SystemWiring] Error unwiring systems: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Apply Harmony patches for game system modifications
        /// </summary>
        private static void ApplyHarmonyPatches()
        {
            try
            {
                var harmony = new Harmony("gg.deca.VAutomation");
                
                // Apply VBlood repair system patch
                harmony.PatchAll(typeof(Patches.RepairVBloodProgressionSystemPatch));
                
                // Apply map icon spawn system patch
                harmony.PatchAll(typeof(Patches.MapIconSpawnSystemPatch));
                
                Log?.LogInfo("[SystemWiring] Applied Harmony patches");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[SystemWiring] Failed to apply Harmony patches: {ex.Message}");
            }
        }
    }
}
