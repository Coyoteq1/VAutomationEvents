using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using BepInEx.Logging;
using ProjectM;

namespace VAuto.Core
{
    /// <summary>
    /// Represents a dedicated zone (spawnable base).
    /// Handles lifecycle, snapshots, and player management.
    /// </summary>
    public class LifecycleZone
    {
        public readonly int ZoneId;
        public readonly string Name;

        public List<Entity> PlayersInZone { get; } = new();
        public ArenaPosition ZonePosition { get; }
        public bool IsActive { get; private set; }

        private readonly Dictionary<Entity, ArenaSnapshot> _snapshots = new();

        public ArenaZone(int id, string name, ArenaPosition position)
        {
            ZoneId = id;
            Name = name;
            ZonePosition = position;
        }

        #region Lifecycle

        /// <summary>
        /// Activate the arena: spawns structures and prepares snapshot system.
        /// </summary>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;

            Plugin.Logger?.LogInfo($"ArenaZone [{ZoneId}] '{Name}' activated at {ZonePosition.Position}.");

            // Optionally: spawn prefabs using EntityManager here
        }

        /// <summary>
        /// Deactivate the arena: cleans up entities and snapshots.
        /// </summary>
        public void Deactivate(EntityManager em)
        {
            if (!IsActive) return;

            foreach (var player in PlayersInZone)
            {
                RestoreSnapshot(player, em);
            }

            PlayersInZone.Clear();
            _snapshots.Clear();

            IsActive = false;
            Plugin.Logger?.LogInfo($"ArenaZone [{ZoneId}] '{Name}' deactivated.");
        }

        #endregion

        #region Player Entry / Exit

        /// <summary>
        /// Player enters the arena.
        /// Takes a snapshot for restoration on exit.
        /// </summary>
        public void Enter(Entity player, EntityManager em)
        {
            if (!IsActive || PlayersInZone.Contains(player)) return;

            // Take snapshot
            _snapshots[player] = ArenaSnapshot.Create(player, em);

            PlayersInZone.Add(player);
            Plugin.Logger?.LogInfo($"Player {player.Index} entered ArenaZone [{ZoneId}] '{Name}'.");

            // Move player to arena position (simplified)
            em.SetComponentData(player, new Translation { Value = ZonePosition.Position });
        }

        /// <summary>
        /// Player exits the arena.
        /// Restores their snapshot.
        /// </summary>
        public void Exit(Entity player, EntityManager em)
        {
            if (!PlayersInZone.Contains(player)) return;

            RestoreSnapshot(player, em);

            PlayersInZone.Remove(player);
            _snapshots.Remove(player);

            Plugin.Logger?.LogInfo($"Player {player.Index} exited ArenaZone [{ZoneId}] '{Name}'.");
        }

        #endregion

        #region Snapshot Handling

        /// <summary>
        /// Restore a player's state from snapshot.
        /// </summary>
        private void RestoreSnapshot(Entity player, EntityManager em)
        {
            if (!_snapshots.TryGetValue(player, out var snapshot)) return;

            snapshot.Restore(player, em);
        }

        #endregion
    }

    /// <summary>
    /// Simple arena snapshot of player state
    /// </summary>
    public class ArenaSnapshot
    {
        public float3 Position;
        public float Health;
        public float MaxHealth;
        // Extend with Equipment / Blood / Abilities

        private ArenaSnapshot() { }

        /// <summary>
        /// Capture player state from ECS
        /// </summary>
        public static ArenaSnapshot Create(Entity player, EntityManager em)
        {
            var snapshot = new ArenaSnapshot();

            if (em.HasComponent<Translation>(player))
                snapshot.Position = em.GetComponentData<Translation>(player).Value;

            if (em.HasComponent<Health>(player))
            {
                var health = em.GetComponentData<Health>(player);
                snapshot.Health = health.Value;
                snapshot.MaxHealth = health.MaxHealth;
            }

            // TODO: capture Equipment / Blood / Abilities

            return snapshot;
        }

        /// <summary>
        /// Restore player state in ECS
        /// </summary>
        public void Restore(Entity player, EntityManager em)
        {
            if (em.HasComponent<Translation>(player))
                em.SetComponentData(player, new Translation { Value = Position });

            if (em.HasComponent<Health>(player))
                em.SetComponentData(player, new Health { Value = Health, MaxHealth = MaxHealth });

            // TODO: restore Equipment / Blood / Abilities
        }
    }


}