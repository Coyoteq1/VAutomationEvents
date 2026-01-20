using System;
using System.Collections.Generic;
using Unity.Entities;

namespace VAuto.Extensions
{
    /// <summary>
    /// EntityMapper - Entity serialization and mapping system
    /// Based on KindredSchematics implementation patterns for efficient entity management
    /// </summary>
    public class EntityMapper
    {
        private readonly Dictionary<Entity, int> _entityIndexLookup = new();
        private readonly List<Entity> _entities = new();

        /// <summary>
        /// Default constructor - creates empty mapper with null entity at index 0
        /// </summary>
        public EntityMapper()
        {
            AddEntity(Entity.Null);
        }

        /// <summary>
        /// Constructor with initial entity list
        /// </summary>
        /// <param name="entitiesToAdd">Entities to initialize with</param>
        public EntityMapper(IEnumerable<Entity> entitiesToAdd)
        {
            _entityIndexLookup = new Dictionary<Entity, int>();
            _entities = new List<Entity>();

            // Always add null entity at index 0
            AddEntity(Entity.Null);

            // Add provided entities
            foreach (var entity in entitiesToAdd)
                AddEntity(entity);
        }

        /// <summary>
        /// Add entity to mapper and return its index
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>Index of the entity</returns>
        /// <exception cref="ArgumentException">Thrown if trying to add entity with Prefab component</exception>
        public int AddEntity(Entity entity)
        {
            // Prevent adding entities with Prefab component (they shouldn't be serialized)
            if (entity.Has<Prefab>(VAuto.Core.VRCore.EM))
                throw new ArgumentException("Cannot add entities with Prefab component to EntityMapper");

            // Check if entity already exists in mapping
            if (_entityIndexLookup.TryGetValue(entity, out var existingIndex))
            {
                return existingIndex;
            }

            // Add new entity
            var newIndex = _entities.Count;
            _entityIndexLookup[entity] = newIndex;
            _entities.Add(entity);
            return newIndex;
        }

        /// <summary>
        /// Get entity by index
        /// </summary>
        /// <param name="index">Index of entity</param>
        /// <returns>Entity at index</returns>
        public Entity this[int index] => _entities[index];

        /// <summary>
        /// Get total number of entities in mapper
        /// </summary>
        public int Count => _entities.Count;

        /// <summary>
        /// Get index of entity, adding it if not found
        /// </summary>
        /// <param name="entity">Entity to find</param>
        /// <returns>Index of entity</returns>
        public int IndexOf(Entity entity)
        {
            if (_entityIndexLookup.TryGetValue(entity, out var index))
                return index;
            return AddEntity(entity);
        }

        /// <summary>
        /// Check if entity exists in mapper
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>True if entity exists</returns>
        public bool Contains(Entity entity)
        {
            return _entityIndexLookup.ContainsKey(entity);
        }

        /// <summary>
        /// Get all entities as array
        /// </summary>
        /// <returns>Array of all entities</returns>
        public Entity[] ToArray()
        {
            return _entities.ToArray();
        }

        /// <summary>
        /// Clear all entities except null entity at index 0
        /// </summary>
        public void Clear()
        {
            _entityIndexLookup.Clear();
            _entities.Clear();
            AddEntity(Entity.Null);
        }

        /// <summary>
        /// Get entity at index with validation
        /// </summary>
        /// <param name="index">Index to get</param>
        /// <returns>Entity at index or Entity.Null if invalid</returns>
        public Entity GetEntity(int index)
        {
            if (index >= 0 && index < _entities.Count)
                return _entities[index];
            return Entity.Null;
        }

        /// <summary>
        /// Get index with validation
        /// </summary>
        /// <param name="entity">Entity to find</param>
        /// <returns>Index or -1 if not found</returns>
        public int TryGetIndex(Entity entity)
        {
            return _entityIndexLookup.TryGetValue(entity, out var index) ? index : -1;
        }

        /// <summary>
        /// Remove entity from mapper
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        /// <returns>True if entity was removed</returns>
        public bool Remove(Entity entity)
        {
            if (entity == Entity.Null || entity.Index == 0)
                return false; // Never remove null entity

            if (_entityIndexLookup.TryGetValue(entity, out var index))
            {
                _entityIndexLookup.Remove(entity);
                _entities.RemoveAt(index);
                
                // Update indices for entities after the removed one
                var keysToUpdate = new List<Entity>();
                foreach (var kvp in _entityIndexLookup)
                {
                    if (kvp.Value > index)
                        keysToUpdate.Add(kvp.Key);
                }

                foreach (var key in keysToUpdate)
                {
                    _entityIndexLookup[key]--;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Get serialization statistics
        /// </summary>
        /// <returns>Statistics about the mapper</returns>
        public EntityMapperStatistics GetStatistics()
        {
            int nullEntities = 0;
            int prefabEntities = 0;
            int castleHeartEntities = 0;
            int validEntities = 0;

            foreach (var entity in _entities)
            {
                if (entity == Entity.Null)
                    nullEntities++;
                else if (entity.Has<Prefab>(VAuto.Core.VRCore.EM))
                    prefabEntities++;
                else
                    validEntities++;
            }

            return new EntityMapperStatistics
            {
                TotalEntities = _entities.Count,
                NullEntities = nullEntities,
                PrefabEntities = prefabEntities,
                CastleHeartEntities = castleHeartEntities,
                ValidEntities = validEntities,
                UniqueEntities = _entityIndexLookup.Count
            };
        }
    }

    /// <summary>
    /// EntityMapper statistics for debugging and monitoring
    /// </summary>
    public struct EntityMapperStatistics
    {
        public int TotalEntities;
        public int NullEntities;
        public int PrefabEntities;
        public int CastleHeartEntities;
        public int ValidEntities;
        public int UniqueEntities;

        public override string ToString()
        {
            return $"EntityMapperStats: Total={TotalEntities}, Valid={ValidEntities}, Null={NullEntities}, Prefab={PrefabEntities}, CastleHeart={CastleHeartEntities}, Unique={UniqueEntities}";
        }
    }

    /// <summary>
    /// Extension methods for Entity to work with EntityMapper
    /// </summary>
    public static class EntityMapperExtensions
    {
        /// <summary>
        /// Check if entity should be excluded from serialization
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>True if entity should be excluded</returns>
        public static bool ShouldExcludeFromSerialization(this Entity entity)
        {
            return entity.Has<Prefab>(VAuto.Core.VRCore.EM);
        }

        /// <summary>
        /// Get a safe index for entity in mapper (excluded entities get index 0)
        /// </summary>
        /// <param name="entity">Entity to get index for</param>
        /// <param name="mapper">EntityMapper to use</param>
        /// <returns>Safe index for entity</returns>
        public static int GetSafeMapperIndex(this Entity entity, EntityMapper mapper)
        {
            if (entity.ShouldExcludeFromSerialization())
                return 0; // Use null entity index for excluded entities

            return mapper.IndexOf(entity);
        }

        /// <summary>
        /// Check if entity is valid for mapper operations
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>True if entity is valid</returns>
        public static bool IsValidForMapper(this Entity entity)
        {
            return entity != Entity.Null && !entity.ShouldExcludeFromSerialization();
        }
    }
}












