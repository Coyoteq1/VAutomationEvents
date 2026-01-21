using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using VAuto.Core;

namespace VAuto.Patches
{
    /// <summary>
    /// Harmony patch for MapIconSpawnSystem to make map icons globally visible
    /// </summary>
    [HarmonyPatch(typeof(MapIconSpawnSystem), nameof(MapIconSpawnSystem.OnUpdate))]
    internal static class MapIconSpawnSystemPatch
    {
        public static void Prefix(MapIconSpawnSystem __instance)
        {
            try
            {
                var entities = __instance.__query_1050583545_0.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (!entity.Has<Attach>()) continue;

                    var attachParent = entity.Read<Attach>().Parent;
                    if (attachParent.Equals(Entity.Null)) continue;

                    if (!attachParent.Has<SpawnedBy>()) continue;

                    var mapIconData = entity.Read<MapIconData>();

                    // Make icons globally visible for all players
                    mapIconData.RequiresReveal = false;
                    mapIconData.AllySetting = MapIconShowSettings.Global;
                    mapIconData.EnemySetting = MapIconShowSettings.Global;
                    entity.Write(mapIconData);

                    VRCore.Logger?.LogDebug($"[MapIconPatch] Made icon globally visible for entity {entity.Index}");
                }
                entities.Dispose();
            }
            catch (System.Exception ex)
            {
                VRCore.Logger?.LogError($"[MapIconPatch] Error in patch: {ex.Message}");
            }
        }
    }
}