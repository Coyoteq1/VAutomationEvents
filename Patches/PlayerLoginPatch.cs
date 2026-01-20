using HarmonyLib;
using ProjectM.Network;
using Unity.Entities;
using VAuto.Services;
using VAuto.Core;
using BepInEx.Logging;

namespace VAuto.Patches
{
    /// <summary>
    /// Harmony patch for player login events.
    /// Triggers the Dual Character setup via DualCharacterManager.
    /// </summary>
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class PlayerLoginPatch
    {
        private static ManualLogSource Log => Plugin.Logger;

        public static void Postfix(ServerBootstrapSystem __instance, Entity userEntity)
        {
            try
            {
                if (userEntity == Entity.Null || !VRCore.EM.Exists(userEntity)) return;

                if (!VRCore.EM.TryGetComponentData(userEntity, out User user))
                {
                    Log?.LogWarning($"[LoginPatch] User entity {userEntity.Index} has no User component");
                    return;
                }

                ulong platformId = user.PlatformId;
                Entity characterEntity = user.LocalCharacter._Entity;

                Log?.LogInfo($"[LoginPatch] Player connected: {user.CharacterName} (PlatformId: {platformId})");

                // Trigger Dual Character setup
                DualCharacterManager.OnPlayerLogin(userEntity, characterEntity, platformId);
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"[LoginPatch] Error in OnUserConnected Postfix: {ex.Message}");
            }
        }
    }
}