using System;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using ProjectM.Network;
using BepInEx.Logging;

namespace VAuto.Services
{
    /// <summary>
    /// Service for freezing and unfreezing characters.
    /// Freezing involves removing network ownership and moving the character to a hidden location.
    /// </summary>
    public static class CharacterFreezeService
    {
        private static ManualLogSource Log => Plugin.Logger;
        private static readonly float3 HiddenLocation = new float3(-1000, 5, -500);

        private static void FastTeleport(Entity entity, float3 position)
        {
            var em = VAutoCore.EntityManager;
            if (entity != Entity.owner && em.Exists(entity))
            {
                if (em.HasComponent<Unity.Transforms.Translation>(entity))
                    em.SetComponentData(entity, new Unity.Transforms.Translation { Value = position });
                else
                    em.AddComponentData(entity, new Unity.Transforms.Translation { Value = position });
            }
        }

        /// <summary>
        /// Freezes a character: removes network component and teleports underground.
        /// </summary>
        public static void Freeze(Entity character)
        {
            if (character == Entity.Null || !VRCore.EM.Exists(character)) return;

            try
            {
                // 1. Remove network ownership
                if (VRCore.EM.HasComponent<FromCharacter>(character))
                {
                    VRCore.EM.RemoveComponent<FromCharacter>(character);
                }

                // 2. Teleport to hidden location
                FastTeleport(character, HiddenLocation);

                // 3. Add Frozen tag
                VRCore.EM.AddComponent<VAutoFrozenCharacterTag>(character);

                Log?.LogInfo($"[FreezeService] Character {character.Index} frozen and moved to {HiddenLocation}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[FreezeService] Error freezing character {character.Index}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unfreezes a character: restores network ownership and removes frozen tag.
        /// </summary>
        public static void Unfreeze(Entity userEntity, Entity character)
        {
            if (character == Entity.Null || !VRCore.EM.Exists(character)) return;

            try
            {
                // 1. Restore network ownership
                if (!VRCore.EM.HasComponent<FromCharacter>(character))
                {
                    VRCore.EM.AddComponentData(character, new FromCharacter { User = userEntity });
                }

                // 2. Remove Frozen tag
                if (VRCore.EM.HasComponent<VAutoFrozenCharacterTag>(character))
                {
                    VRCore.EM.RemoveComponent<VAutoFrozenCharacterTag>(character);
                }

                Log?.LogInfo($"[FreezeService] Character {character.Index} unfrozen for user {userEntity.Index}");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[FreezeService] Error unfreezing character {character.Index}: {ex.Message}");
            }
        }
    }
}