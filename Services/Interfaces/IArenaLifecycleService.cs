using Unity.Entities;
using ProjectM;
using ProjectM.Network;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Simplified lifecycle interface for arena services.
    /// Only used by LifecycleService for internal coordination.
    /// </summary>
    public interface IArenaLifecycleService
    {
        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        bool OnPlayerEnter(Entity user, Entity character, string arenaId);

        /// <summary>
        /// Called when a player exits the arena
        /// </summary>
        bool OnPlayerExit(Entity user, Entity character, string arenaId);

        /// <summary>
        /// Called when an arena starts
        /// </summary>
        bool OnArenaStart(string arenaId);

        /// <summary>
        /// Called when an arena ends
        /// </summary>
        bool OnArenaEnd(string arenaId);

        /// <summary>
        /// Called when building starts
        /// </summary>
        bool OnBuildStart(Entity user, string structureName, string arenaId);

        /// <summary>
        /// Called when building completes
        /// </summary>
        bool OnBuildComplete(Entity user, string structureName, string arenaId);

        /// <summary>
        /// Called when building is destroyed
        /// </summary>
        bool OnBuildDestroy(Entity user, string structureName, string arenaId);
    }
}
