using System;
using System.Collections.Generic;
using Unity.Entities;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Services
{
    public static class ComponentInitializer
    {
        private static readonly Dictionary<string, Type> _registeredExtractors = new();

        public static void InitializeComponents()
        {
            // Core components
            RegisterExtractor("Entity", typeof(Entity));
            RegisterExtractor("Translation", typeof(Translation));
            RegisterExtractor("Rotation", typeof(Rotation));
            RegisterExtractor("Scale", typeof(Scale));
            RegisterExtractor("LocalToWorld", typeof(LocalToWorld));
            RegisterExtractor("PrefabGUID", typeof(PrefabGUID));

            // ProjectM components
            RegisterExtractor("User", typeof(ProjectM.User));
            RegisterExtractor("PlayerCharacter", typeof(ProjectM.PlayerCharacter));
            RegisterExtractor("Buff", typeof(ProjectM.Buff));
            RegisterExtractor("Equipment", typeof(ProjectM.Equipment));

            // Castle components
            RegisterExtractor("CastleBuilding", typeof(ProjectM.CastleBuilding));
            RegisterExtractor("CastleHeart", typeof(ProjectM.CastleHeart));

            // Automation Framework Components
            RegisterExtractor("ArenaZone", typeof(ArenaZone));
            RegisterExtractor("BossEntity", typeof(BossEntity));
            RegisterExtractor("RewardData", typeof(RewardData));
            RegisterExtractor("RespawnPoint", typeof(RespawnPoint));
            RegisterExtractor("PlanMarker", typeof(PlanMarker));
            RegisterExtractor("AutomationState", typeof(AutomationState));

            Plugin.Logger?.LogInfo($"[ComponentInitializer] Registered {_registeredExtractors.Count} component extractors");
        }

        private static void RegisterExtractor(string name, Type type)
        {
            _registeredExtractors[name] = type;

            // Register extractor type with EntityDebug
            try
            {
                var registerMethod = typeof(EntityDebug).GetMethod("RegisterExtractor");
                registerMethod?.Invoke(null, new object[] { name, type });
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[ComponentInitializer] Failed to register extractor for {name}: {ex.Message}");
            }
        }

        public static Type GetExtractorType(string componentName)
        {
            return _registeredExtractors.TryGetValue(componentName, out var type) ? type : null;
        }

        public static IEnumerable<string> GetRegisteredComponents()
        {
            return _registeredExtractors.Keys;
        }

        public static bool HasComponent(Entity entity, Type componentType)
        {
            var method = typeof(ComponentInitializer)
                .GetMethod(nameof(HasComponentGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var genericMethod = method?.MakeGenericMethod(componentType);
            return (bool)genericMethod?.Invoke(null, new object[] { entity });
        }

        private static bool HasComponentGeneric<T>(Entity entity) where T : struct, IComponentData
        {
            return VRCore.EM.HasComponent(entity, new ComponentType(Il2CppType.Of<T>()));
        }
    }

    public static class EntityDebugExtractor<T> where T : struct, IComponentData
    {
        public static T? Extract(Entity entity)
        {
            if (VRCore.EM.HasComponent(entity, new ComponentType(Il2CppType.Of<T>())))
            {
                return VRCore.EM.GetComponentData<T>(entity);
            }
            return null;
        }

        public static bool HasComponent(Entity entity)
        {
            return VRCore.EM.HasComponent(entity, new ComponentType(Il2CppType.Of<T>()));
        }
    }
}
