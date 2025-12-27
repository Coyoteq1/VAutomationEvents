using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Arena Glow Service - Manages arena lighting and glow effects
    /// </summary>
    public static class ArenaGlowService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, GlowData> _glowEffects = new();
        private static readonly object _lock = new object();
        
        public static bool IsInitialized => _initialized;
        public static ManualLogSource Log => Plugin.Logger;

        #region Initialization
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;
                
                try
                {
                    Log?.LogInfo("[ArenaGlowService] Initializing arena glow service...");
                    
                    _glowEffects.Clear();
                    InitializeDefaultGlows();
                    _initialized = true;
                    
                    Log?.LogInfo("[ArenaGlowService] Arena glow service initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to initialize: {ex.Message}");
                    throw;
                }
            }
        }

        public static void Cleanup()
        {
            lock (_lock)
            {
                if (!_initialized) return;
                
                try
                {
                    Log?.LogInfo("[ArenaGlowService] Cleaning up arena glow service...");
                    
                    ClearAllGlows();
                    _glowEffects.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[ArenaGlowService] Arena glow service cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to cleanup: {ex.Message}");
                }
            }
        }

        private static void InitializeDefaultGlows()
        {
            // Add default arena glow effects
            CreateGlow("arena_center", new float3(0, 10, 0), GlowType.Circular, 50f, 
                      new float4(0.2f, 0.8f, 1.0f, 0.3f), true);
            CreateGlow("arena_boundary", new float3(0, 2, 0), GlowType.Boundary, 60f, 
                      new float4(1.0f, 0.5f, 0.0f, 0.2f), false);
            CreateGlow("spawn_point", float3.zero, GlowType.Point, 10f, 
                      new float4(0.0f, 1.0f, 0.0f, 0.4f), true);
        }
        #endregion

        #region Glow Management
        public static bool CreateGlow(string name, float3 position, GlowType type, float radius, 
                                    float4 color, bool isActive)
        {
            lock (_lock)
            {
                try
                {
                    if (_glowEffects.ContainsKey(name))
                    {
                        Log?.LogWarning($"[ArenaGlowService] Glow '{name}' already exists");
                        return false;
                    }

                    var glowData = new GlowData
                    {
                        Name = name,
                        Position = position,
                        Type = type,
                        Radius = radius,
                        Color = color,
                        IsActive = isActive,
                        CreatedAt = DateTime.UtcNow,
                        Intensity = 1.0f
                    };

                    _glowEffects[name] = glowData;
                    
                    if (isActive)
                    {
                        ApplyGlowEffect(glowData);
                    }
                    
                    Log?.LogInfo($"[ArenaGlowService] Created glow '{name}' at {position}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to create glow '{name}': {ex.Message}");
                    return false;
                }
            }
        }

        public static bool RemoveGlow(string name)
        {
            lock (_lock)
            {
                try
                {
                    if (!_glowEffects.TryGetValue(name, out var glowData))
                        return false;

                    // Remove the visual effect
                    RemoveGlowEffect(glowData);
                    _glowEffects.Remove(name);
                    
                    Log?.LogInfo($"[ArenaGlowService] Removed glow '{name}'");
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to remove glow '{name}': {ex.Message}");
                    return false;
                }
            }
        }

        public static bool UpdateGlow(string name, float3? position = null, float? radius = null, 
                                    float4? color = null, bool? isActive = null, float? intensity = null)
        {
            lock (_lock)
            {
                try
                {
                    if (!_glowEffects.TryGetValue(name, out var glowData))
                        return false;

                    bool needsUpdate = false;

                    if (position.HasValue)
                    {
                        glowData.Position = position.Value;
                        needsUpdate = true;
                    }

                    if (radius.HasValue)
                    {
                        glowData.Radius = radius.Value;
                        needsUpdate = true;
                    }

                    if (color.HasValue)
                    {
                        glowData.Color = color.Value;
                        needsUpdate = true;
                    }

                    if (isActive.HasValue)
                    {
                        if (glowData.IsActive != isActive.Value)
                        {
                            glowData.IsActive = isActive.Value;
                            needsUpdate = true;
                            
                            if (isActive.Value)
                                ApplyGlowEffect(glowData);
                            else
                                RemoveGlowEffect(glowData);
                        }
                    }

                    if (intensity.HasValue)
                    {
                        glowData.Intensity = intensity.Value;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        Log?.LogInfo($"[ArenaGlowService] Updated glow '{name}'");
                    }

                    return needsUpdate;
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to update glow '{name}': {ex.Message}");
                    return false;
                }
            }
        }
        #endregion

        #region Visual Effects
        private static void ApplyGlowEffect(GlowData glowData)
        {
            try
            {
                // This would typically spawn a visual effect entity or particle system
                // For now, we'll log the effect application
                Log?.LogDebug($"[ArenaGlowService] Applying glow effect: {glowData.Name} at {glowData.Position}");
                
                // TODO: Implement actual visual effect spawning using ProjectM's particle system API
                // This would involve creating entities with particle system components
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaGlowService] Failed to apply glow effect: {ex.Message}");
            }
        }

        private static void RemoveGlowEffect(GlowData glowData)
        {
            try
            {
                // This would typically remove the visual effect entity or disable particle systems
                Log?.LogDebug($"[ArenaGlowService] Removing glow effect: {glowData.Name}");
                
                // TODO: Implement actual visual effect removal
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ArenaGlowService] Failed to remove glow effect: {ex.Message}");
            }
        }

        public static void ClearAllGlows()
        {
            lock (_lock)
            {
                try
                {
                    foreach (var glow in _glowEffects.Values)
                    {
                        if (glow.IsActive)
                        {
                            RemoveGlowEffect(glow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[ArenaGlowService] Failed to clear all glows: {ex.Message}");
                }
            }
        }
        #endregion

        #region Query Methods
        public static List<string> GetAllGlowNames()
        {
            lock (_lock)
            {
                return _glowEffects.Keys.ToList();
            }
        }

        public static GlowData GetGlow(string name)
        {
            lock (_lock)
            {
                return _glowEffects.TryGetValue(name, out var glow) ? glow : null;
            }
        }

        public static int GetActiveGlowCount()
        {
            lock (_lock)
            {
                return _glowEffects.Values.Count(g => g.IsActive);
            }
        }

        public static List<GlowData> GetGlowsByType(GlowType type)
        {
            lock (_lock)
            {
                return _glowEffects.Values.Where(g => g.Type == type).ToList();
            }
        }

        public static List<GlowData> GetGlowsInRange(float3 center, float radius)
        {
            lock (_lock)
            {
                return _glowEffects.Values.Where(g => 
                    math.distance(g.Position, center) <= radius).ToList();
            }
        }
        #endregion

        #region Data Structures
        public enum GlowType
        {
            Point,
            Circular,
            Boundary,
            Linear,
            Area
        }

        public class GlowData
        {
            public string Name { get; set; }
            public float3 Position { get; set; }
            public GlowType Type { get; set; }
            public float Radius { get; set; }
            public float4 Color { get; set; } // RGBA
            public bool IsActive { get; set; }
            public float Intensity { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        #endregion
    }
}