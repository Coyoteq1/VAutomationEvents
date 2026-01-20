using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Il2CppInterop.Runtime;

namespace VAuto.Core
{
    /// <summary>
    /// Core utilities for accessing V Rising game systems
    /// </summary>
    public static class VRisingCore
    {
        private static World _serverWorld;
        private static World Server
        {
            get
            {
                if (_serverWorld == null || !_serverWorld.IsCreated)
                {
                    _serverWorld = GetWorld("Server");
                }
                return _serverWorld;
            }
        }

        public static ProjectM.PrefabCollectionSystem PrefabCollectionSystem => Server?.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>() ?? throw new System.Exception("Server world not available");

        public static World ServerWorld => Server;

        public static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => Server?.GetExistingSystemManaged<ActivateVBloodAbilitySystem>() ?? throw new System.Exception("Server world not available");

        public static ProjectM.Scripting.ServerGameManager ServerGameManager => Server?.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager ?? throw new System.Exception("Server world not available");

        public static DebugEventsSystem DebugEventsSystem => Server?.GetExistingSystemManaged<DebugEventsSystem>() ?? throw new System.Exception("Server world not available");

        public static EntityManager EntityManager => Server?.EntityManager ?? throw new System.Exception("Server world not available");

        /// <summary>
        /// Get a system from the server world (VAMP pattern)
        /// </summary>
        public static T GetSystem<T>() where T : SystemBase
        {
            return Server.GetExistingSystemManaged<T>();
        }

        /// <summary>
        /// VAMP-style safe system access with null check
        /// </summary>
        public static bool TryGetSystem<T>(out T system) where T : SystemBase
        {
            system = default;
            try
            {
                system = Server.GetExistingSystemManaged<T>();
                return system != null;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to get system {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }
    }
}
