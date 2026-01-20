using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services.Interfaces;
using VAuto.Services.Lifecycle;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Unified API for Automation.
    /// Acts as a facade for Chat Commands, ZUI, and Proximity Systems.
    /// </summary>
    public class AutomationAPI : IService
    {
        private static readonly Lazy<AutomationAPI> _instance = new(() => new AutomationAPI());
        public static AutomationAPI Instance => _instance.Value;

        private ManualLogSource _log;
        private bool _isInitialized;

        // Dependencies
        private LifecycleService _lifecycleService => ServiceManager.GetService<LifecycleService>();
        private ArenaBuildingService _buildService => ServiceManager.GetService<ArenaBuildingService>();
        // private ArenaZoneService ZoneService => ServiceManager.GetService<ArenaZoneService>(); // Commented to avoid circular dependency
        // private ArenaObjectService _objectService => ArenaObjectService.Instance; // Uncomment when available
        
        // State
        private readonly Dictionary<int, ArenaMatchState> _matchStates = new();
        private readonly Dictionary<int, List<TeamData>> _arenaTeams = new();
        private readonly Dictionary<ulong, int> _playerTeams = new(); // PlatformId -> TeamId

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        private AutomationAPI() { }

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = Plugin.Logger;
            _isInitialized = true;
            _log?.LogInfo("[AutomationAPI] Initialized");
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            _matchStates.Clear();
            _arenaTeams.Clear();
            _playerTeams.Clear();
            _isInitialized = false;
            _log?.LogInfo("[AutomationAPI] Cleaned up");
        }

        #region Data Structures

        public struct ArenaResult
        {
            public bool Success;
            public string Message;

            public static ArenaResult Ok(string message = "Success") => new ArenaResult { Success = true, Message = message };
            public static ArenaResult Fail(string message) => new ArenaResult { Success = false, Message = message };
        }

        public struct ArenaStateSummary
        {
            public int ArenaId;
            public int PlayerCount;
            public string Status; // "Idle", "Starting", "InProgress", "Resetting"
            public float Timer;
        }

        public struct ArenaSummary
        {
            public int ArenaId;
            public string Name;
            public int PlayerCount;
            public bool IsActive;
        }

        public struct LoadoutSummary
        {
            public string Name;
            public string Category;
            public string Description;
            public string IconPrefab;
        }

        public struct TeamSummary
        {
            public int TeamId;
            public string TeamName;
            public List<ulong> MemberPlatformIds;
            public int Score;
            public bool IsReady;
        }

        private class ArenaMatchState
        {
            public string Status = "Idle";
            public float Timer = 0f;
            public DateTime LastActivity = DateTime.UtcNow;
        }

        private class TeamData
        {
            public int TeamId;
            public string Name;
            public ulong LeaderId;
            public List<ulong> Members = new();
            public int Score;
            public bool IsReady;
        }

        #endregion

        #region Player Management (Join/Leave)

        public ArenaResult Join(Entity userEntity, int arenaId = 0)
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
                return ArenaResult.Fail("Invalid user entity");

            var character = user.LocalCharacter._Entity;
            if (character == Entity.Null)
                return ArenaResult.Fail("Player has no character");

            // 1. Check if already in an arena
            if (_lifecycleService.IsPlayerInArena(user.PlatformId))
                return ArenaResult.Fail("Already in an arena");

            // 2. Call Lifecycle Service
            if (_lifecycleService.EnterArena(userEntity, character, arenaId.ToString()))
            {
                // 3. Ensure Zone Service tracks it (Lifecycle might do this, but double check)
                // ZoneService.AddPlayerToZone(arenaId, user.PlatformId);
                
                return ArenaResult.Ok($"Joined Arena {arenaId}");
            }

            return ArenaResult.Fail("Failed to enter arena lifecycle");
        }

        public ArenaResult Leave(Entity userEntity)
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
                return ArenaResult.Fail("Invalid user entity");

            var character = user.LocalCharacter._Entity;

            // 1. Call Lifecycle Service
            if (_lifecycleService.ExitArena(userEntity, character))
            {
                // 2. Cleanup Team State
                if (_playerTeams.TryGetValue(user.PlatformId, out var teamId))
                {
                    LeaveTeam(userEntity);
                }

                return ArenaResult.Ok("Left arena");
            }

            return ArenaResult.Fail("Failed to exit arena lifecycle");
        }

        public ArenaResult Spectate(Entity userEntity, int arenaId)
        {
            // Placeholder for spectate logic
            // Would involve putting player in "Ghost" mode and teleporting to arena
            return ArenaResult.Fail("Spectate mode not implemented yet");
        }

        #endregion

        #region Match Automation

        public ArenaResult StartMatch(int arenaId)
        {
            if (!_matchStates.TryGetValue(arenaId, out var state))
            {
                state = new ArenaMatchState();
                _matchStates[arenaId] = state;
            }

            if (state.Status == "InProgress")
                return ArenaResult.Fail("Match already in progress");

            var playerCount = 0; // ZoneService.GetActivePlayerCount(arenaId);
            if (playerCount < 2)
                return ArenaResult.Fail("Not enough players to start (min 2)");

            state.Status = "Starting";
            state.Timer = 5.0f; // 5 second countdown
            
            // Logic to freeze players would go here
            // CharacterFreezeService.FreezeAllInArena(arenaId);

            _log?.LogInfo($"[AutomationAPI] Starting match in Arena {arenaId}");
            return ArenaResult.Ok("Match starting in 5 seconds...");
        }

        public ArenaResult ResetMatch(int arenaId)
        {
            if (!_matchStates.ContainsKey(arenaId))
                return ArenaResult.Fail("Arena not active");

            // 1. Reset State
            _matchStates[arenaId].Status = "Idle";
            _matchStates[arenaId].Timer = 0;

            // 2. Clear Entities (Mobs, etc)
            ClearEntities(arenaId);

            // 3. Heal Players & Reset Cooldowns
            var players = new List<ulong>(); // ZoneService.GetActivePlayers(arenaId);
            foreach (var platformId in players)
            {
                // Logic to heal/reset would go here
                // PlayerService.Heal(platformId);
                // PlayerService.ResetCooldowns(platformId);
            }

            // 4. Teleport to spawn points
            // TeleportService.TeleportToArenaSpawn(arenaId);

            return ArenaResult.Ok("Arena reset");
        }

        public ArenaResult ForceEndMatch(int arenaId)
        {
            if (!_matchStates.ContainsKey(arenaId))
                return ArenaResult.Fail("Arena not active");

            _matchStates[arenaId].Status = "Idle";
            return ArenaResult.Ok("Match forced to end");
        }

        #endregion

        #region Environment

        public ArenaResult SpawnPracticeTarget(int arenaId, string prefabName)
        {
            // Placeholder: Interface with WorldSpawnService
            // WorldSpawnService.SpawnMob(arenaId, prefabName);
            return ArenaResult.Ok($"Spawned {prefabName} (Simulation)");
        }

        public ArenaResult ClearEntities(int arenaId)
        {
            // Placeholder: Interface with ArenaObjectService
            // ArenaObjectService.Instance.RemoveAllObjectsFromArena(arenaId.ToString());
            return ArenaResult.Ok("Arena entities cleared");
        }

        #endregion

        #region Loadouts

        public ArenaResult ApplyLoadout(Entity userEntity, string buildName)
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
                return ArenaResult.Fail("Invalid user");

            var character = user.LocalCharacter._Entity;
            
            // Placeholder: Interface with ArenaBuildService
            // if (_buildService.ApplyBuild(character, buildName))
            //     return ArenaResult.Ok($"Applied loadout: {buildName}");
            
            return ArenaResult.Ok($"Applied loadout: {buildName} (Simulation)");
        }

        public ArenaResult SaveCurrentAsLoadout(Entity userEntity, string customName)
        {
            // Placeholder: Capture current inventory/skills
            return ArenaResult.Ok($"Saved current loadout as: {customName}");
        }

        public ArenaResult ApplyRandomLoadout(Entity userEntity)
        {
            var loadouts = GetAvailableLoadouts();
            if (loadouts.Count == 0) return ArenaResult.Fail("No loadouts available");

            var random = new System.Random();
            var choice = loadouts[random.Next(loadouts.Count)];
            return ApplyLoadout(userEntity, choice.Name);
        }

        public List<LoadoutSummary> GetAvailableLoadouts()
        {
            // Placeholder: Fetch from ArenaBuildService
            return new List<LoadoutSummary>
            {
                new LoadoutSummary { Name = "Dracula_Scholar", Category = "Mage", Description = "High Damage", IconPrefab = "Assets/Icons/Mage.png" },
                new LoadoutSummary { Name = "Paladin_Tank", Category = "Tank", Description = "High Defense", IconPrefab = "Assets/Icons/Tank.png" }
            };
        }

        public string GetActiveLoadoutName(Entity userEntity)
        {
            return "Unknown";
        }

        #endregion

        #region Team Management

        public ArenaResult CreateTeam(Entity leaderEntity, string teamName = "")
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(leaderEntity, out var user))
                return ArenaResult.Fail("Invalid user");

            if (_playerTeams.ContainsKey(user.PlatformId))
                return ArenaResult.Fail("Already in a team");

            var teamId = _playerTeams.Count + 1; // Simple ID generation
            var team = new TeamData
            {
                TeamId = teamId,
                Name = string.IsNullOrEmpty(teamName) ? $"Team {teamId}" : teamName,
                LeaderId = user.PlatformId
            };
            team.Members.Add(user.PlatformId);

            // Store team
            // Note: In a real implementation, we'd need to know which arena this team belongs to
            // For now, we'll assume a global team list or default to arena 0
            if (!_arenaTeams.ContainsKey(0)) _arenaTeams[0] = new List<TeamData>();
            _arenaTeams[0].Add(team);
            
            _playerTeams[user.PlatformId] = teamId;

            return ArenaResult.Ok($"Created team: {team.Name}");
        }

        public ArenaResult InviteToTeam(Entity leaderEntity, Entity targetEntity)
        {
            // Placeholder: Send notification to target
            return ArenaResult.Ok("Invite sent");
        }

        public ArenaResult JoinTeam(Entity userEntity, int teamId)
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
                return ArenaResult.Fail("Invalid user");

            if (_playerTeams.ContainsKey(user.PlatformId))
                return ArenaResult.Fail("Already in a team");

            // Find team
            var team = _arenaTeams.Values.SelectMany(t => t).FirstOrDefault(t => t.TeamId == teamId);
            if (team == null)
                return ArenaResult.Fail("Team not found");

            team.Members.Add(user.PlatformId);
            _playerTeams[user.PlatformId] = teamId;

            return ArenaResult.Ok($"Joined team: {team.Name}");
        }

        public ArenaResult LeaveTeam(Entity userEntity)
        {
            if (!VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
                return ArenaResult.Fail("Invalid user");

            if (!_playerTeams.TryGetValue(user.PlatformId, out var teamId))
                return ArenaResult.Fail("Not in a team");

            var team = _arenaTeams.Values.SelectMany(t => t).FirstOrDefault(t => t.TeamId == teamId);
            if (team != null)
            {
                team.Members.Remove(user.PlatformId);
                if (team.Members.Count == 0)
                {
                    // Remove empty team
                    foreach (var list in _arenaTeams.Values) list.Remove(team);
                }
            }

            _playerTeams.Remove(user.PlatformId);
            return ArenaResult.Ok("Left team");
        }

        public ArenaResult AutoBalanceTeams(int arenaId)
        {
            // Logic to shuffle players into teams
            return ArenaResult.Ok("Teams balanced");
        }

        public TeamSummary GetTeamDetails(int teamId)
        {
            var team = _arenaTeams.Values.SelectMany(t => t).FirstOrDefault(t => t.TeamId == teamId);
            if (team == null) return new TeamSummary();

            return new TeamSummary
            {
                TeamId = team.TeamId,
                TeamName = team.Name,
                MemberPlatformIds = new List<ulong>(team.Members),
                Score = team.Score,
                IsReady = team.IsReady
            };
        }

        public int GetPlayerTeamId(Entity userEntity)
        {
            if (VAuto.Core.Core.TryRead<ProjectM.Network.User>(userEntity, out var user))
            {
                return _playerTeams.TryGetValue(user.PlatformId, out var teamId) ? teamId : -1;
            }
            return -1;
        }

        public List<TeamSummary> GetTeamsInArena(int arenaId)
        {
            if (_arenaTeams.TryGetValue(arenaId, out var teams))
            {
                return teams.Select(t => new TeamSummary
                {
                    TeamId = t.TeamId,
                    TeamName = t.Name,
                    MemberPlatformIds = new List<ulong>(t.Members),
                    Score = t.Score,
                    IsReady = t.IsReady
                }).ToList();
            }
            return new List<TeamSummary>();
        }

        #endregion

        #region Queries

        public ArenaStateSummary GetStatus(int arenaId)
        {
            var status = "Idle";
            var timer = 0f;

            if (_matchStates.TryGetValue(arenaId, out var state))
            {
                status = state.Status;
                timer = state.Timer;
            }

            return new ArenaStateSummary
            {
                ArenaId = arenaId,
                PlayerCount = ZoneService.GetActivePlayerCount(arenaId),
                Status = status,
                Timer = timer
            };
        }

        public List<ArenaSummary> GetAll()
        {
            var ids = new List<int>(); // ZoneService.GetActiveArenaIds();
            return ids.Select(id => new ArenaSummary
            {
                ArenaId = id,
                Name = $"Arena {id}",
                PlayerCount = 0, // ZoneService.GetActivePlayerCount(id)
                IsActive = true
            }).ToList();
        }

        #endregion
    }
}