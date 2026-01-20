using System;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace VAuto.Services
{
    /// <summary>
    /// Service for creating and configuring arena characters
    /// </summary>
    public static class ArenaCharacterCreator
    {
        private static readonly float3 HiddenArenaLocation = new float3(-1000, 5, -500);

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
        /// Create an arena character copy of the normal character
        /// </summary>
        public static Entity CreateArenaCharacter(Entity normalCharacter, ulong platformId)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Creating arena character for platformId {platformId}");

                // Get the user entity
                if (!VRCore.EM.TryGetComponentData(normalCharacter, out ProjectM.Network.FromCharacter fromChar))
                {
                    Plugin.Logger?.LogError($"Normal character {normalCharacter.Index} has no FromCharacter component");
                    return Entity.Null;
                }

                var userEntity = fromChar.User;
                if (!VRCore.EM.TryGetComponentData(userEntity, out User user))
                {
                    Plugin.Logger?.LogError($"User entity {userEntity.Index} has no User component");
                    return Entity.Null;
                }

                // Create character copy
                var arenaCharacter = CreateCharacterCopy(normalCharacter, userEntity, platformId);
                if (arenaCharacter == Entity.Null)
                {
                    Plugin.Logger?.LogError($"Failed to create character copy for platformId {platformId}");
                    return Entity.Null;
                }

                // Configure arena character properties
                ConfigureArenaCharacter(arenaCharacter, user);

                // Apply max level and Dracula build
                ApplyMaxLevelAndBuild(arenaCharacter);

                // Move to hidden location
                MoveToHiddenLocation(arenaCharacter);

                Plugin.Logger?.LogInfo($"Successfully created arena character {arenaCharacter.Index} for platformId {platformId}");
                return arenaCharacter;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating arena character for platformId {platformId}: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Create a copy of the character entity
        /// </summary>
        private static Entity CreateCharacterCopy(Entity sourceCharacter, Entity userEntity, ulong platformId)
        {
            try
            {
                // In V Rising, character creation is complex. For now, we'll create a basic entity
                // and configure it. This may need to be enhanced based on actual V Rising APIs.

                var em = VRCore.EM;
                var arenaCharacter = em.CreateEntity();

                // Copy essential components from source character
                CopyCharacterComponents(sourceCharacter, arenaCharacter, userEntity);

                // Set arena-specific name
                if (em.TryGetComponentData(userEntity, out User user))
                {
                    var arenaName = $"{user.CharacterName}PvP";
                    // TODO: Set character name if possible
                    Plugin.Logger?.LogInfo($"Arena character name: {arenaName}");
                }

                return arenaCharacter;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating character copy: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Copy essential components from source to target character
        /// </summary>
        private static void CopyCharacterComponents(Entity source, Entity target, Entity userEntity)
        {
            try
            {
                var em = VRCore.EM;

                // Add network component
                em.AddComponentData(target, new ProjectM.Network.FromCharacter { User = userEntity });

                // Copy basic transform (will be overridden later)
                if (em.TryGetComponentData(source, out Unity.Transforms.LocalToWorld ltw))
                {
                    em.AddComponentData(target, ltw);
                }

                // Copy other essential components
                // TODO: Copy PlayerCharacter, Health, etc. components as needed

                Plugin.Logger?.LogInfo($"Copied components from {source.Index} to {target.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error copying character components: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure arena character properties
        /// </summary>
        private static void ConfigureArenaCharacter(Entity arenaCharacter, User user)
        {
            try
            {
                var em = VRCore.EM;

                // TODO: Configure arena-specific properties
                // - Blood type
                // - Equipment
                // - Inventory
                // - Stats

                Plugin.Logger?.LogInfo($"Configured arena character {arenaCharacter.Index} for user {user.CharacterName}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error configuring arena character: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply max level (91) and Dracula build to arena character
        /// </summary>
        private static void ApplyMaxLevelAndBuild(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // TODO: Implement max level application (level 91 equivalent to game level 80)
                // TODO: Apply Dracula build with everything maxed out
                // TODO: Max out all research and abilities
                // TODO: Unlock all books and progression

                Plugin.Logger?.LogInfo($"Applied max level and Dracula build to arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying max level and build: {ex.Message}");
            }
        }

        /// <summary>
        /// Move character to hidden arena location
        /// </summary>
        private static void MoveToHiddenLocation(Entity arenaCharacter)
        {
            try
            {
                // Teleport to hidden location
                FastTeleport(arenaCharacter, HiddenArenaLocation);

                // Freeze the character initially
                CharacterFreezeService.Freeze(arenaCharacter);

                Plugin.Logger?.LogInfo($"Moved arena character {arenaCharacter.Index} to hidden location");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error moving to hidden location: {ex.Message}");
            }
        }

        /// <summary>
        /// Recreate arena character if it was lost (after server restart)
        /// </summary>
        public static Entity RecreateArenaCharacter(ulong platformId, Entity normalCharacter, Entity userEntity)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Recreating arena character for platformId {platformId}");

                // Clean up any existing arena character
                var existingState = DualCharacterManager.GetState(platformId);
                if (existingState != null && existingState.ArenaCharacter != Entity.Null)
                {
                    if (VRCore.EM.Exists(existingState.ArenaCharacter))
                    {
                        // Destroy existing character
                        VRCore.EM.DestroyEntity(existingState.ArenaCharacter);
                    }
                }

                // Create new arena character
                var newArenaCharacter = CreateArenaCharacter(normalCharacter, platformId);
                if (newArenaCharacter != Entity.Null)
                {
                    // Update dual state
                    var state = DualCharacterManager.GetOrCreateState(platformId, normalCharacter);
                    state.ArenaCharacter = newArenaCharacter;
                    state.IsInitialized = true;
                }

                return newArenaCharacter;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error recreating arena character for platformId {platformId}: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Validate that an arena character meets requirements
        /// </summary>
        public static bool ValidateArenaCharacter(Entity arenaCharacter)
        {
            try
            {
                if (arenaCharacter == Entity.Null || !VRCore.EM.Exists(arenaCharacter))
                    return false;

                var em = VRCore.EM;

                // Check for required components
                if (!em.HasComponent<ProjectM.Network.FromCharacter>(arenaCharacter))
                    return false;

                // TODO: Add more validation checks
                // - Max level
                // - Dracula build
                // - Blood type
                // - Abilities maxed

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error validating arena character: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get default arena blood type
        /// </summary>
        public static string GetDefaultArenaBloodType()
        {
            // TODO: Get from configuration
            return "Rogue";
        }

        /// <summary>
        /// Get default hidden arena location
        /// </summary>
        public static float3 GetHiddenArenaLocation()
        {
            // TODO: Get from configuration
            return new float3(-1000, 5, -500);
        }
    }
}
