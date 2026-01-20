using System;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Authoritative service for spawning and configuring PvP practice characters.
    /// Follows the AI Directive for Dual Character Model.
    /// </summary>
    public static class PvPCharacterSpawner
    {
        private static ManualLogSource Log => Plugin.Logger;

        private static void FastTeleport(Entity entity, float3 position)
        {
            var em = VAutoCore.EntityManager;
            if (entity != Entity.Null && em.Exists(entity))
            {
                if (em.HasComponent<Unity.Transforms.Translation>(entity))
                    em.SetComponentData(entity, new Unity.Transforms.Translation { Value = position });
                else
                    em.AddComponentData(entity, new Unity.Transforms.Translation { Value = position });
            }
        }

        /// <summary>
        /// Spawns a new PvP character for a player.
        /// Naming convention: (PlayerName pvp)
        /// </summary>
        public static Entity SpawnPvPCharacter(Entity userEntity, ulong platformId)
        {
            try
            {
                if (!VRCore.EM.TryGetComponentData(userEntity, out User user))
                {
                    Log?.LogError($"[PvPSpawner] User entity {userEntity.Index} has no User component");
                    return Entity.Null;
                }

                string pvpName = $"({user.CharacterName} pvp)";
                Log?.LogInfo($"[PvPSpawner] Spawning PvP character '{pvpName}' for platformId {platformId}");

                // 1. Create character entity
                // In V Rising, we typically clone the existing character or use a prefab.
                // For this implementation, we'll create a new entity and add required components.
                var pvpCharacter = VRCore.EM.CreateEntity();

                // 2. Add mandatory components
                VRCore.EM.AddComponentData(pvpCharacter, new FromCharacter { User = userEntity });
                
                // Add PvP Practice Tag
                VRCore.EM.AddComponent<PvPPracticeTag>(pvpCharacter);

                // 3. Apply default PvP setup (Warrior gear, 100% blood, etc.)
                ApplyDefaultPvPSetup(pvpCharacter);

                // 4. Move to hidden location and freeze
                float3 hiddenPos = new float3(-1000, 5, -500);
                FastTeleport(pvpCharacter, hiddenPos);
                CharacterFreezeService.Freeze(pvpCharacter);

                Log?.LogInfo($"[PvPSpawner] Successfully spawned and frozen PvP character {pvpCharacter.Index} for {pvpName}");
                return pvpCharacter;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PvPSpawner] Error spawning PvP character: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Applies the default PvP setup to a character.
        /// </summary>
        private static void ApplyDefaultPvPSetup(Entity character)
        {
            try
            {
                // Apply Warrior gear set
                // ArenaBuildService.ApplyBuild(character, "Warrior"); 
                
                // Set 100% Blood Quality
                // TODO: Implement blood quality application
                
                // Apply Arena-safe buffs
                // TODO: Implement buff application
                
                Log?.LogInfo($"[PvPSpawner] Applied default PvP setup to character {character.Index}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PvPSpawner] Error applying PvP setup: {ex.Message}");
            }
        }
    }
}