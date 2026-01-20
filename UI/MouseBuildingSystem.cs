using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ProjectM;
using VAuto.Core;

namespace VAuto.UI
{
    public class MouseBuildingSystem
    {
        private static MouseBuildingSystem _instance;
        public static MouseBuildingSystem Instance => _instance ??= new MouseBuildingSystem();

        private bool _buildMode = false;
        private BuildObject _selectedObject;
        private Entity _previewEntity = Entity.Null;
        private readonly Dictionary<int, Entity> _placedObjects = new();
        private int _nextObjectId = 0;

        public bool IsBuildModeEnabled => _buildMode;

        public MouseBuildingSystem()
        {
            BuildingUpdateBehavior.Setup();
        }

        public enum BuildType { Tile, Glow, Structure }

        public class BuildObject
        {
            public BuildType Type { get; set; }
            public int PrefabGuid { get; set; }
            public string Name { get; set; }
            public Color GlowColor { get; set; }
        }

        public void EnableBuildMode(BuildType type, int prefabGuid, string name, Color? glowColor = null)
        {
            _buildMode = true;
            _selectedObject = new BuildObject
            {
                Type = type,
                PrefabGuid = prefabGuid,
                Name = name,
                GlowColor = glowColor ?? Color.white
            };
            BuildingUpdateBehavior.Setup();
            Plugin.Logger?.LogInfo($"[MouseBuilding] Build mode enabled: {name}");
        }

        public void DisableBuildMode()
        {
            _buildMode = false;
            _selectedObject = null;
            DestroyPreview();
            Plugin.Logger?.LogInfo("[MouseBuilding] Build mode disabled");
        }

        public void ToggleBuildMode()
        {
            if (_buildMode)
                DisableBuildMode();
            else if (_selectedObject != null)
                EnableBuildMode(_selectedObject.Type, _selectedObject.PrefabGuid, _selectedObject.Name, _selectedObject.GlowColor);
        }

        public void Update()
        {
            if (!_buildMode || _selectedObject == null) return;

            var mousePos = Input.mousePosition;
            var worldPos = ScreenToWorldPosition(mousePos);

            UpdatePreview(worldPos);

            if (Input.GetMouseButtonDown(0)) PlaceObject(worldPos);
            if (Input.GetMouseButtonDown(1)) RemoveObjectAt(worldPos);
            if (Input.GetMouseButton(2)) MoveObjectAt(worldPos);
        }

        private void UpdatePreview(float3 position)
        {
            if (_previewEntity == Entity.Null)
            {
                _previewEntity = CreatePreviewEntity(position);
            }
            else
            {
                SetEntityPosition(_previewEntity, position);
            }
        }

        private Entity CreatePreviewEntity(float3 position)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new Unity.Transforms.Translation { Value = position });
                return entity;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[MouseBuilding] Error creating preview: {ex.Message}");
                return Entity.Null;
            }
        }

        private void PlaceObject(float3 position)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new Unity.Transforms.Translation { Value = position });
                
                // Apply color to both tiles and glows
                if (_selectedObject.Type == BuildType.Glow)
                {
                    ApplyGlow(entity, _selectedObject.GlowColor);
                }
                else if (_selectedObject.Type == BuildType.Tile)
                {
                    ApplyTileColor(entity, _selectedObject.GlowColor);
                }
                
                _placedObjects[_nextObjectId++] = entity;
                Plugin.Logger?.LogInfo($"[MouseBuilding] Placed {_selectedObject.Name} at {position}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[MouseBuilding] Error placing object: {ex.Message}");
            }
        }

        private void RemoveObjectAt(float3 position)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                Entity closestEntity = Entity.Null;
                float closestDist = 5f;

                foreach (var kvp in _placedObjects)
                {
                    if (em.Exists(kvp.Value))
                    {
                        var pos = GetEntityPosition(kvp.Value);
                        var dist = math.distance(pos, position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestEntity = kvp.Value;
                        }
                    }
                }

                if (closestEntity != Entity.Null)
                {
                    em.DestroyEntity(closestEntity);
                    Plugin.Logger?.LogInfo($"[MouseBuilding] Removed object");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[MouseBuilding] Error removing: {ex.Message}");
            }
        }

        private void MoveObjectAt(float3 position)
        {
            try
            {
                var em = VAuto.Core.Core.EntityManager;
                Entity closestEntity = Entity.Null;
                float closestDist = 5f;

                foreach (var kvp in _placedObjects)
                {
                    if (em.Exists(kvp.Value))
                    {
                        var pos = GetEntityPosition(kvp.Value);
                        var dist = math.distance(pos, position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestEntity = kvp.Value;
                        }
                    }
                }

                if (closestEntity != Entity.Null)
                {
                    SetEntityPosition(closestEntity, position);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[MouseBuilding] Error moving: {ex.Message}");
            }
        }

        private void ApplyGlow(Entity entity, Color color)
        {
            Plugin.Logger?.LogDebug($"[MouseBuilding] Applied glow {color}");
        }

        private void ApplyTileColor(Entity entity, Color color)
        {
            Plugin.Logger?.LogDebug($"[MouseBuilding] Applied tile color {color}");
        }

        private void DestroyPreview()
        {
            if (_previewEntity != Entity.Null)
            {
                try
                {
                    VAuto.Core.Core.EntityManager.DestroyEntity(_previewEntity);
                    _previewEntity = Entity.Null;
                }
                catch { }
            }
        }

        private float3 ScreenToWorldPosition(Vector3 screenPos)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                var ray = camera.ScreenPointToRay(screenPos);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    var worldPos = ray.GetPoint(distance);
                    return new float3(worldPos.x, worldPos.y, worldPos.z);
                }
            }
            return float3.zero;
        }

        private float3 GetEntityPosition(Entity entity)
        {
            var em = VAuto.Core.Core.EntityManager;
            if (em.HasComponent<Unity.Transforms.Translation>(entity))
            {
                return em.GetComponentData<Unity.Transforms.Translation>(entity).Value;
            }
            return float3.zero;
        }

        private void SetEntityPosition(Entity entity, float3 position)
        {
            var em = VAuto.Core.Core.EntityManager;
            if (em.HasComponent<Unity.Transforms.Translation>(entity))
            {
                em.SetComponentData(entity, new Unity.Transforms.Translation { Value = position });
            }
        }

        public void ClearAll()
        {
            var em = VAuto.Core.Core.EntityManager;
            foreach (var kvp in _placedObjects)
            {
                if (em.Exists(kvp.Value))
                {
                    em.DestroyEntity(kvp.Value);
                }
            }
            _placedObjects.Clear();
            Plugin.Logger?.LogInfo("[MouseBuilding] Cleared all");
        }
    }
}
