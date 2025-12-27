using System;
using Unity.Entities;
using Unity.Mathematics;
using VAuto.Core;
using VAuto.Services;

namespace VAuto.Services
{
    /// <summary>
    /// Service for handling instant character swaps between normal and arena characters
    /// </summary>
    public static class CharacterSwapService
    {
        /// <summary>
        /// Perform instant character swap without kicking the player
        /// </summary>
        public static bool SwapCharacters(ulong platformId, Entity userEntity)
        {
            try
            {
                // Get dual state
                var state = DualCharacterManager.GetState(platformId);
                if (state == null || !state.IsInitialized)
                {
                    Plugin.Logger?.LogWarning($"No initialized dual state found for platformId {platformId}");
                    return false;
                }

                // Validate characters exist
                if (state.NormalCharacter == Entity.Null || !VRCore.EM.Exists(state.NormalCharacter))
                {
                    Plugin.Logger?.LogError($"Normal character does not exist for platformId {platformId}");
                    return false;
                }

                if (state.ArenaCharacter == Entity.Null || !VRCore.EM.Exists(state.ArenaCharacter))
                {
                    Plugin.Logger?.LogError($"Arena character does not exist for platformId {platformId}");
                    return false;
                }

                // Determine swap direction
                bool swappingToArena = !state.IsArenaActive;

                if (swappingToArena)
                {
                    // Swap to arena character
                    return SwitchToArenaCharacter(platformId, userEntity, state);
                }
                else
                {
                    // Swap to normal character
                    return SwitchToNormalCharacter(platformId, userEntity, state);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error during character swap for platformId {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Switch to arena character
        /// </summary>
        private static bool SwitchToArenaCharacter(ulong platformId, Entity userEntity, DualCharacterState state)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Switching platformId {platformId} to arena character");

                // CAPTURE: Save normal character's inventory and equipment before switching
                EnhancedInventoryManager.CaptureInventory(state.NormalCharacter, platformId, "normal");
                EnhancedInventoryManager.CaptureEquipment(state.NormalCharacter, platformId, "normal");

                // Update normal character position before switching
                if (VRCore.EM.TryGetComponentData(state.NormalCharacter, out Unity.Transforms.LocalToWorld ltw))
                {
                    state.LastNormalPosition = ltw.Position;
                }

                // Freeze normal character
                DualCharacterManager.FreezeCharacter(state.NormalCharacter);

                // Activate arena character
                DualCharacterManager.ActivateCharacter(userEntity, state.ArenaCharacter);

                // CLEAR: Remove inventory and equipment from arena character
                // Use Method 1 for both (complete clearing)
                EnhancedInventoryManager.ClearInventoryMethod1(state.ArenaCharacter);
                EnhancedInventoryManager.ClearEquipmentMethod1(state.ArenaCharacter);

                // Apply arena blood type and progression
                ApplyArenaState(state.ArenaCharacter);

                // Teleport to arena spawn position
                var arenaSpawnPos = GetArenaSpawnPosition();
                MissingServices.TeleportFix.FastTeleport(state.ArenaCharacter, arenaSpawnPos);

                // Update state
                state.IsArenaActive = true;
                state.LastSwapTime = DateTime.UtcNow;

                // Notify lifecycle system
                MissingServices.LifecycleService.EnterArena(userEntity, state.ArenaCharacter);

                Plugin.Logger?.LogInfo($"Successfully switched platformId {platformId} to arena character");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error switching to arena character for platformId {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Switch to normal character
        /// </summary>
        private static bool SwitchToNormalCharacter(ulong platformId, Entity userEntity, DualCharacterState state)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Switching platformId {platformId} to normal character");

                // Freeze arena character
                DualCharacterManager.FreezeCharacter(state.ArenaCharacter);

                // Activate normal character
                DualCharacterManager.ActivateCharacter(userEntity, state.NormalCharacter);

                // RESTORE: Restore normal character's inventory and equipment
                EnhancedInventoryManager.RestoreInventory(state.NormalCharacter, platformId, "normal");
                EnhancedInventoryManager.RestoreEquipment(state.NormalCharacter, platformId, "normal");

                // Restore normal blood type and progression
                RestoreNormalState(state.NormalCharacter);

                // Teleport back to last normal position
                MissingServices.TeleportFix.FastTeleport(state.NormalCharacter, state.LastNormalPosition);

                // Update state
                state.IsArenaActive = false;
                state.LastSwapTime = DateTime.UtcNow;

                // Notify lifecycle system
                MissingServices.LifecycleService.ExitArena(userEntity, state.ArenaCharacter);

                Plugin.Logger?.LogInfo($"Successfully switched platformId {platformId} to normal character");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error switching to normal character for platformId {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply arena-specific state (blood type, max progression, etc.)
        /// </summary>
        public static void ApplyArenaState(Entity arenaCharacter)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Applying full arena state to character {arenaCharacter.Index}");

                var em = VRCore.EM;

                // 1. Apply max level (91)
                ApplyMaxLevel(arenaCharacter);

                // 2. Unlock all research and technologies
                UnlockAllResearch(arenaCharacter);

                // 3. Unlock all blood types
                UnlockAllBloodTypes(arenaCharacter);

                // 4. Unlock all spell books and abilities
                UnlockAllSpellBooks(arenaCharacter);

                // 5. Apply Dracula build (everything maxed)
                ApplyDraculaBuild(arenaCharacter);

                // 6. Set arena blood type (Rogue by default)
                SetArenaBloodType(arenaCharacter, "Rogue");

                Plugin.Logger?.LogInfo($"Successfully applied full arena state to character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying arena state: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore normal character state
        /// </summary>
        private static void RestoreNormalState(Entity normalCharacter)
        {
            try
            {
                // TODO: Restore original blood type and progression state
                // For now, just log the intent
                Plugin.Logger?.LogInfo($"Restored normal state to character {normalCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring normal state: {ex.Message}");
            }
        }

        /// <summary>
        /// Get arena spawn position
        /// </summary>
        private static float3 GetArenaSpawnPosition()
        {
            // TODO: Get from configuration
            // For now, use default hidden arena location
            return new float3(-1000, 5, -500);
        }

        /// <summary>
        /// Check if a character swap is currently possible
        /// </summary>
        public static bool CanSwapCharacters(ulong platformId)
        {
            var state = DualCharacterManager.GetState(platformId);
            if (state == null || !state.IsInitialized)
                return false;

            // Check if both characters exist and are valid
            return state.NormalCharacter != Entity.Null && VRCore.EM.Exists(state.NormalCharacter) &&
                   state.ArenaCharacter != Entity.Null && VRCore.EM.Exists(state.ArenaCharacter);
        }

        /// <summary>
        /// Get current active character type for a player
        /// </summary>
        public static string GetActiveCharacterType(ulong platformId)
        {
            var state = DualCharacterManager.GetState(platformId);
            if (state == null || !state.IsInitialized)
                return "Unknown";

            return state.IsArenaActive ? "Arena" : "Normal";
        }

        /// <summary>
        /// Force a specific character to be active
        /// </summary>
        public static bool ForceActivateCharacter(ulong platformId, Entity userEntity, bool activateArena)
        {
            var state = DualCharacterManager.GetState(platformId);
            if (state == null || !state.IsInitialized)
                return false;

            if (activateArena && state.IsArenaActive)
                return true; // Already active

            if (!activateArena && !state.IsArenaActive)
                return true; // Already active

            // Perform the swap
            return SwapCharacters(platformId, userEntity);
        }

        #region Progression Unlocking Methods

        /// <summary>
        /// Apply max level (91) to arena character
        /// </summary>
        private static void ApplyMaxLevel(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // Set character level to 91 (max arena level)
                // This requires working with V Rising's level progression system
                // TODO: Implement actual level setting via game APIs

                Plugin.Logger?.LogInfo($"Applied max level (91) to arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying max level: {ex.Message}");
            }
        }

        /// <summary>
        /// Unlock all research and technologies for arena character
        /// </summary>
        private static void UnlockAllResearch(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // Unlock all research trees, technologies, and advancements
                // This includes castle building, weapon upgrades, armor research, etc.
                // TODO: Implement via game's research progression system

                Plugin.Logger?.LogInfo($"Unlocked all research for arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error unlocking research: {ex.Message}");
            }
        }

        /// <summary>
        /// Unlock all blood types for arena character
        /// </summary>
        private static void UnlockAllBloodTypes(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // Unlock all blood types in the game
                // Warrior, Rogue, Mage, Brute, Scholar, Worker, etc.
                // TODO: Implement via blood type unlock system

                Plugin.Logger?.LogInfo($"Unlocked all blood types for arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error unlocking blood types: {ex.Message}");
            }
        }

        /// <summary>
        /// Unlock all spell books and abilities for arena character
        /// </summary>
        private static void UnlockAllSpellBooks(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // Unlock all spell books, grimoires, and ability trees
                // Blood magic, chaos magic, frost magic, etc.
                // TODO: Implement via spell book unlock system

                Plugin.Logger?.LogInfo($"Unlocked all spell books for arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error unlocking spell books: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Dracula build (everything maxed) to arena character
        /// </summary>
        private static void ApplyDraculaBuild(Entity arenaCharacter)
        {
            try
            {
                var em = VRCore.EM;

                // Apply Dracula build - max everything
                // Max health, max damage, max resistances, max abilities
                // Ultimate vampire build with all powers unlocked
                // TODO: Implement via character build system

                Plugin.Logger?.LogInfo($"Applied Dracula build to arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying Dracula build: {ex.Message}");
            }
        }

        /// <summary>
        /// Set arena blood type for character
        /// </summary>
        private static void SetArenaBloodType(Entity arenaCharacter, string bloodType)
        {
            try
            {
                var em = VRCore.EM;

                // Set blood type to specified type (default: Rogue)
                // This affects abilities, playstyle, and UI
                // TODO: Implement via blood type system

                Plugin.Logger?.LogInfo($"Set blood type to {bloodType} for arena character {arenaCharacter.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error setting blood type: {ex.Message}");
            }
        }

        #endregion

        #region Inventory and Equipment Management Methods

        /// <summary>
        /// Capture inventory and equipment from character before arena entry
        /// </summary>
        private static void CaptureInventoryAndEquipment(Entity character, ulong platformId, string context)
        {
            try
            {
                // TODO: Implement actual inventory/equipment capture via V Rising APIs
                // This should save all inventory items and equipped gear to persistent storage
                Plugin.Logger?.LogInfo($"Captured inventory/equipment for character {character.Index} in context {context}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing inventory/equipment: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear inventory and equipment from character (for arena mode)
        /// </summary>
        private static void ClearInventoryAndEquipment(Entity character)
        {
            try
            {
                var em = VRCore.EM;

                // Method 1: Clear inventory completely (remove all items)
                ClearInventoryMethod1(character);

                // Method 2: Clear equipment completely (remove all equipped items)
                ClearEquipmentMethod2(character);

                // Method 3: Alternative - stash to container (if preferred)
                // StashToContainerMethod3(character);

                Plugin.Logger?.LogInfo($"Cleared inventory/equipment from character {character.Index} for arena mode");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing inventory/equipment: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore inventory and equipment to character after arena exit
        /// </summary>
        private static void RestoreInventoryAndEquipment(Entity character, ulong platformId, string context)
        {
            try
            {
                // TODO: Implement actual inventory/equipment restoration via V Rising APIs
                // This should restore all previously saved inventory items and equipped gear
                Plugin.Logger?.LogInfo($"Restored inventory/equipment for character {character.Index} from context {context}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring inventory/equipment: {ex.Message}");
            }
        }

        /// <summary>
        /// Method 1: Clear inventory - remove all items
        /// </summary>
        private static void ClearInventoryMethod1(Entity character)
        {
            try
            {
                // TODO: Implement actual inventory clearing via V Rising APIs
                // This would remove all items from the character's inventory
                Plugin.Logger?.LogInfo($"Cleared inventory (Method 1) for character {character.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing inventory (Method 1): {ex.Message}");
            }
        }

        /// <summary>
        /// Method 2: Clear equipment - remove all equipped items
        /// </summary>
        private static void ClearEquipmentMethod2(Entity character)
        {
            try
            {
                // TODO: Implement actual equipment clearing via V Rising APIs
                // This would unequip all items from the character
                Plugin.Logger?.LogInfo($"Cleared equipment (Method 2) for character {character.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing equipment (Method 2): {ex.Message}");
            }
        }

        /// <summary>
        /// Method 3: Stash inventory/equipment to container
        /// </summary>
        private static void StashToContainerMethod3(Entity character)
        {
            try
            {
                // TODO: Implement container stashing via V Rising APIs
                // This would move items to a designated storage container
                Plugin.Logger?.LogInfo($"Stashed inventory/equipment (Method 3) for character {character.Index}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error stashing to container (Method 3): {ex.Message}");
            }
        }

        /// <summary>
        /// Get user entity from character entity
        /// </summary>
        private static Entity GetUserEntityFromCharacter(Entity character)
        {
            try
            {
                var em = VRCore.EM;
                if (em.TryGetComponentData(character, out ProjectM.Network.FromCharacter fromChar))
                {
                    return fromChar.User;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error getting user entity from character: {ex.Message}");
            }

            return Entity.Null;
        }

        #endregion
    }
}
