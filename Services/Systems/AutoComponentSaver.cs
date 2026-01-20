using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Gameplay;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Auto Component Saver - Automatically saves and restores component data
    /// </summary>
    public static class AutoComponentSaver
    {
        private static bool _initialized = false;
        private static readonly Dictionary<Entity, ComponentSaveData> _savedComponents = new();
        private static readonly Dictionary<Type, List<string>> _componentHistory = new();
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
                    Log?.LogInfo("[AutoComponentSaver] Initializing auto component saver...");
                    
                    _savedComponents.Clear();
                    _componentHistory.Clear();
                    _initialized = true;
                    
                    Log?.LogInfo("[AutoComponentSaver] Auto component saver initialized successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[AutoComponentSaver] Failed to initialize: {ex.Message}");
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
                    Log?.LogInfo("[AutoComponentSaver] Cleaning up auto component saver...");
                    
                    _savedComponents.Clear();
                    _componentHistory.Clear();
                    _initialized = false;
                    
                    Log?.LogInfo("[AutoComponentSaver] Auto component saver cleaned up successfully");
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[AutoComponentSaver] Failed to cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Component Saving
        public static bool SaveEntityComponents(Entity entity, string saveId = null)
        {
            try
            {
                if (entity == Entity.Null)
                {
                    Log?.LogWarning("[AutoComponentSaver] Cannot save null entity");
                    return false;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (!em.Exists(entity))
                {
                    Log?.LogWarning("[AutoComponentSaver] Entity does not exist");
                    return false;
                }

                lock (_lock)
                {
                    var saveKey = saveId ?? entity.ToString();
                    var componentData = new ComponentSaveData
                    {
                        Entity = entity,
                        SaveId = saveKey,
                        SaveTime = DateTime.UtcNow,
                        ComponentDatas = new Dictionary<Type, object>()
                    };

                    // Save all components on the entity
                    SaveAllComponents(entity, componentData);
                    
                    _savedComponents[entity] = componentData;
                    
                    // Track save history
                    TrackSaveHistory(entity.GetType(), saveKey);
                    
                    Log?.LogDebug($"[AutoComponentSaver] Saved components for entity {saveKey}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Failed to save entity components: {ex.Message}");
                return false;
            }
        }

        private static void SaveAllComponents(Entity entity, ComponentSaveData saveData)
        {
            var em = VAuto.Core.Core.EntityManager;
            
            try
            {
                // Save Transform components
                SaveTransformComponents(entity, saveData);
                
                // Save Gameplay components
                SaveGameplayComponents(entity, saveData);
                
                // Save Network components
                SaveNetworkComponents(entity, saveData);
                
                // Save custom components
                SaveCustomComponents(entity, saveData);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error saving components: {ex.Message}");
            }
        }

        private static void SaveTransformComponents(Entity entity, ComponentSaveData saveData)
        {
            var em = VAuto.Core.Core.EntityManager;
            
            try
            {
                if (em.HasComponent<Translation>(entity))
                {
                    saveData.ComponentDatas[typeof(Translation)] = em.GetComponentData<Translation>(entity);
                }
                
                if (em.HasComponent<Rotation>(entity))
                {
                    saveData.ComponentDatas[typeof(Rotation)] = em.GetComponentData<Rotation>(entity);
                }
                
                if (em.HasComponent<NonUniformScale>(entity))
                {
                    saveData.ComponentDatas[typeof(NonUniformScale)] = em.GetComponentData<NonUniformScale>(entity);
                }
                
                if (em.HasComponent<LocalTransform>(entity))
                {
                    saveData.ComponentDatas[typeof(LocalTransform)] = em.GetComponentData<LocalTransform>(entity);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error saving transform components: {ex.Message}");
            }
        }

        private static void SaveGameplayComponents(Entity entity, ComponentSaveData saveData)
        {
            var em = VAuto.Core.Core.EntityManager;
            
            try
            {
                // Save PlayerCharacter if present
                if (em.HasComponent<PlayerCharacter>(entity))
                {
                    saveData.ComponentDatas[typeof(PlayerCharacter)] = em.GetComponentData<PlayerCharacter>(entity);
                }
                
                // Save Health if present
                if (em.HasComponent<Health>(entity))
                {
                    saveData.ComponentDatas[typeof(Health)] = em.GetComponentData<Health>(entity);
                }
                
                // Save Blood if present
                if (em.HasComponent<Blood>(entity))
                {
                    saveData.ComponentDatas[typeof(Blood)] = em.GetComponentData<Blood>(entity);
                }
                
                // Save other gameplay components as needed
                // This would include mana, stamina, buffs, etc.
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error saving gameplay components: {ex.Message}");
            }
        }

        private static void SaveNetworkComponents(Entity entity, ComponentSaveData saveData)
        {
            var em = VAuto.Core.Core.EntityManager;
            
            try
            {
                // Save User component if present
                if (em.HasComponent<User>(entity))
                {
                    saveData.ComponentDatas[typeof(User)] = em.GetComponentData<User>(entity);
                }
                
                // Save NetworkId if present
                if (em.HasComponent<NetworkId>(entity))
                {
                    saveData.ComponentDatas[typeof(NetworkId)] = em.GetComponentData<NetworkId>(entity);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error saving network components: {ex.Message}");
            }
        }

        private static void SaveCustomComponents(Entity entity, ComponentSaveData saveData)
        {
            // Save custom mod components
            // This would save any custom components added by the mod
        }
        #endregion

        #region Component Restoration
        public static bool RestoreEntityComponents(Entity entity, string saveId = null)
        {
            try
            {
                if (entity == Entity.Null)
                {
                    Log?.LogWarning("[AutoComponentSaver] Cannot restore to null entity");
                    return false;
                }

                var em = VAuto.Core.Core.EntityManager;
                if (!em.Exists(entity))
                {
                    Log?.LogWarning("[AutoComponentSaver] Target entity does not exist");
                    return false;
                }

                lock (_lock)
                {
                    ComponentSaveData saveData = null;
                    
                    if (!string.IsNullOrEmpty(saveId))
                    {
                        // Find by saveId
                        saveData = _savedComponents.Values.FirstOrDefault(s => s.SaveId == saveId);
                    }
                    else
                    {
                        // Find by entity
                        _savedComponents.TryGetValue(entity, out saveData);
                    }
                    
                    if (saveData == null)
                    {
                        Log?.LogWarning($"[AutoComponentSaver] No saved data found for entity");
                        return false;
                    }

                    // Restore components
                    var success = RestoreAllComponents(entity, saveData);
                    
                    if (success)
                    {
                        Log?.LogDebug($"[AutoComponentSaver] Successfully restored components for entity {saveData.SaveId}");
                    }
                    else
                    {
                        Log?.LogError($"[AutoComponentSaver] Failed to restore some components for entity {saveData.SaveId}");
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Failed to restore entity components: {ex.Message}");
                return false;
            }
        }

        private static bool RestoreAllComponents(Entity entity, ComponentSaveData saveData)
        {
            var em = VAuto.Core.Core.EntityManager;
            var success = true;
            
            try
            {
                foreach (var componentKvp in saveData.ComponentDatas)
                {
                    try
                    {
                        var componentType = componentKvp.Key;
                        var componentData = componentKvp.Value;
                        
                        // Restore based on component type
                        if (RestoreTransformComponent(entity, componentType, componentData, em))
                            continue;
                        else if (RestoreGameplayComponent(entity, componentType, componentData, em))
                            continue;
                        else if (RestoreNetworkComponent(entity, componentType, componentData, em))
                            continue;
                        else if (RestoreCustomComponent(entity, componentType, componentData, em))
                            continue;
                        else
                            Log?.LogWarning($"[AutoComponentSaver] Unknown component type: {componentType.Name}");
                    }
                    catch (Exception ex)
                    {
                        Log?.LogError($"[AutoComponentSaver] Error restoring component {componentKvp.Key.Name}: {ex.Message}");
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error in restore process: {ex.Message}");
                success = false;
            }
            
            return success;
        }

        private static bool RestoreTransformComponent(Entity entity, Type componentType, object componentData, EntityManager em)
        {
            try
            {
                switch (componentType.Name)
                {
                    case "Translation":
                        if (em.HasComponent<Translation>(entity))
                            em.SetComponentData(entity, (Translation)componentData);
                        else
                            em.AddComponentData(entity, (Translation)componentData);
                        return true;
                        
                    case "Rotation":
                        if (em.HasComponent<Rotation>(entity))
                            em.SetComponentData(entity, (Rotation)componentData);
                        else
                            em.AddComponentData(entity, (Rotation)componentData);
                        return true;
                        
                    case "NonUniformScale":
                        if (em.HasComponent<NonUniformScale>(entity))
                            em.SetComponentData(entity, (NonUniformScale)componentData);
                        else
                            em.AddComponentData(entity, (NonUniformScale)componentData);
                        return true;
                        
                    case "LocalTransform":
                        if (em.HasComponent<LocalTransform>(entity))
                            em.SetComponentData(entity, (LocalTransform)componentData);
                        else
                            em.AddComponentData(entity, (LocalTransform)componentData);
                        return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error restoring transform component {componentType.Name}: {ex.Message}");
            }
            
            return false;
        }

        private static bool RestoreGameplayComponent(Entity entity, Type componentType, object componentData, EntityManager em)
        {
            try
            {
                switch (componentType.Name)
                {
                    case "PlayerCharacter":
                        if (em.HasComponent<PlayerCharacter>(entity))
                            em.SetComponentData(entity, (PlayerCharacter)componentData);
                        else
                            em.AddComponentData(entity, (PlayerCharacter)componentData);
                        return true;
                        
                    case "Health":
                        if (em.HasComponent<Health>(entity))
                            em.SetComponentData(entity, (Health)componentData);
                        else
                            em.AddComponentData(entity, (Health)componentData);
                        return true;
                        
                    case "Blood":
                        if (em.HasComponent<Blood>(entity))
                            em.SetComponentData(entity, (Blood)componentData);
                        else
                            em.AddComponentData(entity, (Blood)componentData);
                        return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error restoring gameplay component {componentType.Name}: {ex.Message}");
            }
            
            return false;
        }

        private static bool RestoreNetworkComponent(Entity entity, Type componentType, object componentData, EntityManager em)
        {
            try
            {
                switch (componentType.Name)
                {
                    case "User":
                        if (em.HasComponent<User>(entity))
                            em.SetComponentData(entity, (User)componentData);
                        else
                            em.AddComponentData(entity, (User)componentData);
                        return true;
                        
                    case "NetworkId":
                        if (em.HasComponent<NetworkId>(entity))
                            em.SetComponentData(entity, (NetworkId)componentData);
                        else
                            em.AddComponentData(entity, (NetworkId)componentData);
                        return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[AutoComponentSaver] Error restoring network component {componentType.Name}: {ex.Message}");
            }
            
            return false;
        }

        private static bool RestoreCustomComponent(Entity entity, Type componentType, object componentData, EntityManager em)
        {
            // Restore custom mod components
            return false;
        }
        #endregion

        #region Management Methods
        public static void DeleteSavedComponents(Entity entity)
        {
            lock (_lock)
            {
                if (_savedComponents.Remove(entity))
                {
                    Log?.LogDebug($"[AutoComponentSaver] Deleted saved components for entity");
                }
            }
        }

        public static void DeleteSavedComponentsById(string saveId)
        {
            lock (_lock)
            {
                var itemsToRemove = _savedComponents.Where(kvp => kvp.Value.SaveId == saveId).ToList();
                
                foreach (var item in itemsToRemove)
                {
                    _savedComponents.Remove(item.Key);
                }
                
                Log?.LogDebug($"[AutoComponentSaver] Deleted {itemsToRemove.Count} saved component sets for saveId {saveId}");
            }
        }

        public static void ClearAllSavedComponents()
        {
            lock (_lock)
            {
                _savedComponents.Clear();
                _componentHistory.Clear();
                Log?.LogInfo("[AutoComponentSaver] Cleared all saved components");
            }
        }

        public static List<string> GetSavedComponentIds()
        {
            lock (_lock)
            {
                return _savedComponents.Values.Select(s => s.SaveId).Distinct().ToList();
            }
        }

        public static ComponentSaveData GetSavedComponents(Entity entity)
        {
            lock (_lock)
            {
                return _savedComponents.TryGetValue(entity, out var saveData) ? saveData : null;
            }
        }

        public static ComponentSaveData GetSavedComponentsById(string saveId)
        {
            lock (_lock)
            {
                return _savedComponents.Values.FirstOrDefault(s => s.SaveId == saveId);
            }
        }

        public static int GetSavedComponentCount()
        {
            lock (_lock)
            {
                return _savedComponents.Count;
            }
        }

        private static void TrackSaveHistory(Type entityType, string saveId)
        {
            if (!_componentHistory.ContainsKey(entityType))
            {
                _componentHistory[entityType] = new List<string>();
            }
            
            _componentHistory[entityType].Add(saveId);
            
            // Keep only the last 100 saves per type
            if (_componentHistory[entityType].Count > 100)
            {
                _componentHistory[entityType].RemoveAt(0);
            }
        }

        public static Dictionary<Type, List<string>> GetComponentHistory()
        {
            lock (_lock)
            {
                return new Dictionary<Type, List<string>>(_componentHistory);
            }
        }
        #endregion

        #region Data Structures
        public class ComponentSaveData
        {
            public Entity Entity { get; set; }
            public string SaveId { get; set; }
            public DateTime SaveTime { get; set; }
            public Dictionary<Type, object> ComponentDatas { get; set; } = new();
        }
        #endregion
    }
}