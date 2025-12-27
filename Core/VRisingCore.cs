using Unity.Entities;
using UnityEngine;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;

namespace VAuto.Core
{
    /// <summary>
    /// Core utilities for accessing V Rising game systems
    /// </summary>
    public static class VRisingCore
    {
        private static World Server => GetWorld("Server") ?? throw new System.Exception("There is no Server world.");

        public static PrefabCollectionSystem PrefabCollectionSystem => Server.GetExistingSystemManaged<PrefabCollectionSystem>();

        public static World ServerWorld => Server;

        public static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => Server.GetExistingSystemManaged<ActivateVBloodAbilitySystem>();

        public static ServerGameManager ServerGameManager => Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;

        public static DebugEventsSystem DebugEventsSystem => Server.GetExistingSystemManaged<DebugEventsSystem>();

        public static EntityManager EntityManager => Server.EntityManager;

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












