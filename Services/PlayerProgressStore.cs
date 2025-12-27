using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Player Progress Model - Represents a player's progression data
    /// </summary>
    public class PlayerProgressModel
    {
        public ulong PlatformId { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public float Experience { get; set; } = 0f;
        public Dictionary<string, bool> UnlockedVBloods { get; set; } = new();
        public Dictionary<string, int> AbilityLevels { get; set; } = new();
        public List<string> CompletedQuests { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static PlayerProgressModel CreateDefault(ulong platformId)
        {
            return new PlayerProgressModel
            {
                PlatformId = platformId,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Player Progress Store - JSON-based persistence for player progression data
    /// </summary>
    public static class PlayerProgressStore
    {
        private const string BaseDir = "BepInEx/config/VAuto.Arena";
        private const string FileName = "player_progress.json";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly object FileLock = new();
        private static Dictionary<ulong, PlayerProgressModel> _cache = new();
        private static bool _initialized = false;

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                Directory.CreateDirectory(BaseDir);
                var path = GetFilePath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<Dictionary<ulong, PlayerProgressModel>>(json, JsonOpts);
                    _cache = data ?? new Dictionary<ulong, PlayerProgressModel>();
                }
                else
                {
                    _cache = new Dictionary<ulong, PlayerProgressModel>();
                }

                _initialized = true;
                Plugin.Logger?.LogInfo($"[PlayerProgressStore] Initialized at {path} with {_cache.Count} cached players");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerProgressStore] Failed to initialize: {ex.Message}");
                _cache = new Dictionary<ulong, PlayerProgressModel>();
                _initialized = true; // Mark as initialized even on error to prevent repeated attempts
            }
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            try
            {
                // Save any pending changes before cleanup
                SaveInternal();
                _cache.Clear();
                _initialized = false;
                Plugin.Logger?.LogInfo("[PlayerProgressStore] Cleaned up successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerProgressStore] Failed to cleanup: {ex.Message}");
            }
        }

        private static string GetFilePath()
        {
            return Path.Combine(BaseDir, FileName);
        }

        public static PlayerProgressModel GetOrCreate(ulong platformId)
        {
            if (!_initialized) Initialize();

            lock (FileLock)
            {
                if (!_cache.TryGetValue(platformId, out var progress))
                {
                    progress = PlayerProgressModel.CreateDefault(platformId);
                    _cache[platformId] = progress;
                    SaveInternal();
                }

                return progress;
            }
        }

        public static PlayerProgressModel Get(ulong platformId)
        {
            if (!_initialized) Initialize();

            lock (FileLock)
            {
                _cache.TryGetValue(platformId, out var progress);
                return progress;
            }
        }

        public static void Save(PlayerProgressModel progress)
        {
            if (progress == null) return;
            if (!_initialized) Initialize();

            lock (FileLock)
            {
                progress.LastUpdated = DateTime.UtcNow;
                _cache[progress.PlatformId] = progress;
                SaveInternal();
            }
        }

        public static void Remove(ulong platformId)
        {
            if (!_initialized) Initialize();

            lock (FileLock)
            {
                if (_cache.Remove(platformId))
                {
                    SaveInternal();
                    Plugin.Logger?.LogInfo($"[PlayerProgressStore] Removed progress for platform ID {platformId}");
                }
            }
        }

        public static Dictionary<ulong, PlayerProgressModel> GetAll()
        {
            if (!_initialized) Initialize();

            lock (FileLock)
            {
                return new Dictionary<ulong, PlayerProgressModel>(_cache);
            }
        }

        public static int GetCachedPlayerCount()
        {
            if (!_initialized) return 0;

            lock (FileLock)
            {
                return _cache.Count;
            }
        }

        private static void SaveInternal()
        {
            try
            {
                var path = GetFilePath();
                var json = JsonSerializer.Serialize(_cache, JsonOpts);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[PlayerProgressStore] Failed to save: {ex.Message}");
            }
        }

        public static void ForceSave()
        {
            if (!_initialized) return;

            lock (FileLock)
            {
                SaveInternal();
            }
        }
    }
}
