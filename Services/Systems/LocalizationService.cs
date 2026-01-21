using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Localization Service - Loads and provides localized strings from embedded JSON
    /// </summary>
    public static class LocalizationService
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();
        private static Dictionary<string, string> _localization = new();

        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        /// <summary>
        /// Initialize the localization service
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                LoadLocalization();
                _initialized = true;
                Log?.LogInfo($"[LocalizationService] Initialized with {_localization.Count} localized strings");
            }
        }

        /// <summary>
        /// Cleanup the localization service
        /// </summary>
        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;

                _localization.Clear();
                _initialized = false;
                Log?.LogInfo("[LocalizationService] Cleaned up");
            }
        }

        /// <summary>
        /// Load localization from embedded JSON resource
        /// </summary>
        private static void LoadLocalization()
        {
            try
            {
                var resourceName = "VAuto.Localization.English.json";

                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string jsonContent = reader.ReadToEnd();
                        _localization = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent)
                                      ?? new Dictionary<string, string>();
                    }

                    Log?.LogInfo($"[LocalizationService] Loaded {_localization.Count} localized strings from embedded resource");
                }
                else
                {
                    Log?.LogWarning("[LocalizationService] Embedded localization resource not found, using empty dictionary");
                    _localization = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[LocalizationService] Failed to load localization: {ex.Message}");
                _localization = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Get localized string by GUID/key
        /// </summary>
        public static string GetLocalization(string guid)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_localization.TryGetValue(guid, out var text))
            {
                return text;
            }

            Log?.LogWarning($"[LocalizationService] Localization not found for key: {guid}");
            return $"<Localization not found: {guid}!>";
        }

        /// <summary>
        /// Check if a localization key exists
        /// </summary>
        public static bool HasLocalization(string guid)
        {
            return _localization.ContainsKey(guid);
        }

        /// <summary>
        /// Get all available localization keys
        /// </summary>
        public static IEnumerable<string> GetAllKeys()
        {
            return _localization.Keys;
        }

        /// <summary>
        /// Get localization count
        /// </summary>
        public static int GetLocalizationCount()
        {
            return _localization.Count;
        }
    }
}