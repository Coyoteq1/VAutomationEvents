using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Stunlock.Core;
using VAuto.Services.Interfaces;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// ECS-safe boss reward binding service
    /// Manages boss entities and their reward configurations without violating PvP lifecycle
    /// </summary>
    public class BossRewardBindingService : IService, IServiceHealthMonitor
    {
        private static BossRewardBindingService _instance;
        public static BossRewardBindingService Instance => _instance ??= new BossRewardBindingService();

        private bool _isInitialized;
        private ManualLogSource _log;

        // Boss reward configurations (read-only after initialization)
        private readonly Dictionary<int, BossRewardConfig> _bossRewards = new();

        public bool IsInitialized => _isInitialized;
        public ManualLogSource Log => _log;

        public void Initialize()
        {
            if (_isInitialized) return;
            _log = ServiceManager.Log;

            // Load boss reward configurations
            LoadBossRewardConfigs();

            _isInitialized = true;
            _log?.LogInfo($"[BossRewardBindingService] Initialized with {_bossRewards.Count} boss reward configs");
        }

        public void Cleanup()
        {
            _bossRewards.Clear();
            _isInitialized = false;
        }

        private void LoadBossRewardConfigs()
        {
            // Load from configuration or database
            // For now, initialize with known VBlood bosses
            InitializeDefaultBossRewards();
        }

        private void InitializeDefaultBossRewards()
        {
            // Alpha Wolf
            _bossRewards[-1905777458] = new BossRewardConfig
            {
                BossGuidHash = -1905777458,
                Rewards = new List<RewardEntry>
                {
                    new RewardEntry { ItemGuid = new PrefabGUID(333222111), Quantity = 1, DropChance = 0.8f }, // Blood Essence
                    new RewardEntry { ItemGuid = new PrefabGUID(-1506458059), Quantity = 2, DropChance = 0.5f } // Copper Sword
                }
            };

            // Errol the Stonebreaker
            _bossRewards[-1541423745] = new BossRewardConfig
            {
                BossGuidHash = -1541423745,
                Rewards = new List<RewardEntry>
                {
                    new RewardEntry { ItemGuid = new PrefabGUID(333222111), Quantity = 3, DropChance = 1.0f }, // Blood Essence
                    new RewardEntry { ItemGuid = new PrefabGUID(-1506458054), Quantity = 1, DropChance = 0.7f } // Iron Axe
                }
            };

            // Add more boss configs as needed
        }

        /// <summary>
        /// Get reward configuration for a boss
        /// </summary>
        public BossRewardConfig GetBossRewards(int bossGuidHash)
        {
            return _bossRewards.TryGetValue(bossGuidHash, out var config) ? config : null;
        }

        /// <summary>
        /// Check if boss has reward configuration
        /// </summary>
        public bool HasBossRewards(int bossGuidHash)
        {
            return _bossRewards.ContainsKey(bossGuidHash);
        }

        /// <summary>
        /// Add reward configuration for a boss (admin only, configuration time)
        /// </summary>
        public bool AddBossRewardConfig(int bossGuidHash, BossRewardConfig config)
        {
            if (config == null || config.BossGuidHash != bossGuidHash)
                return false;

            _bossRewards[bossGuidHash] = config;
            _log?.LogInfo($"[BossRewardBindingService] Added reward config for boss {bossGuidHash}");
            return true;
        }

        /// <summary>
        /// Remove reward configuration for a boss
        /// </summary>
        public bool RemoveBossRewardConfig(int bossGuidHash)
        {
            if (_bossRewards.Remove(bossGuidHash))
            {
                _log?.LogInfo($"[BossRewardBindingService] Removed reward config for boss {bossGuidHash}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculate rewards for boss defeat (ECS-safe, returns data only)
        /// </summary>
        public List<RewardResult> CalculateRewards(int bossGuidHash, System.Random random = null)
        {
            var config = GetBossRewards(bossGuidHash);
            if (config == null)
                return new List<RewardResult>();

            random ??= new System.Random();
            var results = new List<RewardResult>();

            foreach (var reward in config.Rewards)
            {
                if (random.NextDouble() <= reward.DropChance)
                {
                    results.Add(new RewardResult
                    {
                        ItemGuid = reward.ItemGuid,
                        Quantity = reward.Quantity
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Validate reward configuration
        /// </summary>
        public bool ValidateRewardConfig(BossRewardConfig config)
        {
            if (config == null) return false;
            if (config.BossGuidHash == 0) return false;
            if (config.Rewards == null) return false;

            foreach (var reward in config.Rewards)
            {
                if (reward.ItemGuid.GuidHash == 0) return false;
                if (reward.Quantity <= 0) return false;
                if (reward.DropChance < 0 || reward.DropChance > 1) return false;
            }

            return true;
        }

        /// <summary>
        /// Get all boss reward configurations
        /// </summary>
        public IEnumerable<KeyValuePair<int, BossRewardConfig>> GetAllBossRewards()
        {
            return _bossRewards;
        }

        public ServiceHealthStatus GetHealthStatus()
        {
            return new ServiceHealthStatus
            {
                ServiceName = "BossRewardBindingService",
                IsHealthy = _isInitialized,
                Status = _isInitialized ? "Active" : "Inactive",
                LastCheck = DateTime.UtcNow
            };
        }

        public ServicePerformanceMetrics GetPerformanceMetrics()
        {
            return new ServicePerformanceMetrics
            {
                ServiceName = "BossRewardBindingService",
                ActiveOperations = _bossRewards.Count,
                MeasuredAt = DateTime.UtcNow
            };
        }

        public int GetErrorCount() => 0;
        public string GetLastError() => null;
    }

    // Boss Reward Data Structures

    public class BossRewardConfig
    {
        public int BossGuidHash { get; set; }
        public List<RewardEntry> Rewards { get; set; } = new();
    }

    public class RewardEntry
    {
        public PrefabGUID ItemGuid { get; set; }
        public int Quantity { get; set; }
        public float DropChance { get; set; } // 0.0 to 1.0
    }

    public class RewardResult
    {
        public PrefabGUID ItemGuid { get; set; }
        public int Quantity { get; set; }
    }
}