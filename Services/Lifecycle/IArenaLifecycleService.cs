using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using VAuto.Data;

namespace VAuto.Services.Lifecycle
{
    /// <summary>
    /// Core lifecycle interface for all arena services
    /// </summary>
    public interface IArenaLifecycleService
    {
        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="character">Character entity</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if entry was successful</returns>
        bool OnPlayerEnter(Entity user, Entity character, string arenaId);

        /// <summary>
        /// Called when a player exits the arena
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="character">Character entity</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if exit was successful</returns>
        bool OnPlayerExit(Entity user, Entity character, string arenaId);

        /// <summary>
        /// Called when arena lifecycle starts
        /// </summary>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if start was successful</returns>
        bool OnArenaStart(string arenaId);

        /// <summary>
        /// Called when arena lifecycle ends
        /// </summary>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if end was successful</returns>
        bool OnArenaEnd(string arenaId);

        /// <summary>
        /// Called when building lifecycle starts
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="structureName">Structure name</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if build start was successful</returns>
        bool OnBuildStart(Entity user, string structureName, string arenaId);

        /// <summary>
        /// Called when building lifecycle completes
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="structureName">Structure name</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if build completion was successful</returns>
        bool OnBuildComplete(Entity user, string structureName, string arenaId);

        /// <summary>
        /// Called when building lifecycle is destroyed
        /// </summary>
        /// <param name="user">User entity</param>
        /// <param name="structureName">Structure name</param>
        /// <param name="arenaId">Arena identifier</param>
        /// <returns>True if build destruction was successful</returns>
        bool OnBuildDestroy(Entity user, string structureName, string arenaId);
    }

    /// <summary>
    /// Lifecycle event data for player events
    /// </summary>
    public class PlayerLifecycleEvent
    {
        public Entity UserEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public ulong PlatformId { get; set; }
        public string CharacterName { get; set; }
        public string ArenaId { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public PlayerLifecycleEventType EventType { get; set; }
        public System.DateTime Timestamp { get; set; }
        public Dictionary<string, object> EventData { get; set; } = new();
    }

    /// <summary>
    /// Lifecycle event data for building events
    /// </summary>
    public class BuildingLifecycleEvent
    {
        public Entity UserEntity { get; set; }
        public string StructureName { get; set; }
        public string ArenaId { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public BuildingLifecycleEventType EventType { get; set; }
        public System.DateTime Timestamp { get; set; }
        public Dictionary<string, object> EventData { get; set; } = new();
    }

    /// <summary>
    /// Lifecycle event types for players
    /// </summary>
    public enum PlayerLifecycleEventType
    {
        Enter,
        Exit,
        Teleport,
        Respawn,
        Death,
        ZoneChange,
        BuildStart,
        BuildComplete
    }

    /// <summary>
    /// Lifecycle event types for buildings
    /// </summary>
    public enum BuildingLifecycleEventType
    {
        Start,
        Complete,
        Destroy,
        Activate,
        Deactivate,
        Transfer
    }
}