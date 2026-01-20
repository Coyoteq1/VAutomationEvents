using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using VAuto.Services.Systems;
using BepInEx.Logging;

namespace VAuto.Services.ECS
{
    /// <summary>
    /// ECS System for managing arena glow effects
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct ArenaGlowSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Initialize glow service
            ArenaGlowService.Instance.Initialize();
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!ArenaGlowService.Instance.IsInitialized)
                    return;

                var deltaTime = SystemAPI.Time.DeltaTime;
                var time = SystemAPI.Time.ElapsedTime;

                // Update dynamic glow effects
                UpdateDynamicGlowEffects(time, deltaTime);

                // Update glow timers and lifecycle
                ArenaGlowService.Instance.OnUpdate((float)deltaTime);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error in OnUpdate: {ex.Message}");
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private static void UpdateDynamicGlowEffects(double time, float deltaTime)
        {
            try
            {
                var activeGlows = ArenaGlowService.GetAllGlowNames();
                
                foreach (var glowName in activeGlows)
                {
                    var glowData = ArenaGlowService.GetGlow(glowName);
                    if (glowData == null)
                        continue;

                    // Handle different glow types
                    switch (glowData.Type)
                    {
                        case GlowType.Circular:
                            UpdateCircularGlow(glowData, time, deltaTime);
                            break;
                            
                        case GlowType.Boundary:
                            UpdateBoundaryGlow(glowData, time, deltaTime);
                            break;
                            
                        case GlowType.Point:
                            UpdatePointGlow(glowData, time, deltaTime);
                            break;
                            
                        case GlowType.Linear:
                            UpdateLinearGlow(glowData, time, deltaTime);
                            break;
                            
                        case GlowType.Area:
                            UpdateAreaGlow(glowData, time, deltaTime);
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating glow effects: {ex.Message}");
            }
        }

        private static void UpdateCircularGlow(GlowData glow, double time, float deltaTime)
        {
            try
            {
                // Pulsating effect for circular glows
                var pulsateSpeed = 2.0f;
                var pulsateAmount = 0.3f;
                var baseIntensity = glow.Intensity;
                var pulsatedIntensity = baseIntensity + (float)math.sin(time * pulsateSpeed) * pulsateAmount * baseIntensity;
                
                var newColor = new float4(glow.Color.x, glow.Color.y, glow.Color.z, pulsatedIntensity * glow.Color.w);
                ArenaGlowService.UpdateGlow(glow.Name, intensity: pulsatedIntensity, color: newColor);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating circular glow: {ex.Message}");
            }
        }

        private static void UpdateBoundaryGlow(GlowData glow, double time, float deltaTime)
        {
            try
            {
                // Subtle pulsing for boundary glows
                var pulseSpeed = 0.5f;
                var pulseAmount = 0.1f;
                var baseIntensity = glow.Intensity;
                var pulsedIntensity = baseIntensity + (float)math.sin(time * pulseSpeed) * pulseAmount * baseIntensity;
                
                ArenaGlowService.UpdateGlow(glow.Name, intensity: pulsedIntensity);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating boundary glow: {ex.Message}");
            }
        }

        private static void UpdatePointGlow(GlowData glow, double time, float deltaTime)
        {
            try
            {
                // Flickering effect for point glows
                var flickerSpeed = 8.0f;
                var flickerAmount = 0.2f;
                var baseIntensity = glow.Intensity;
                var flickeredIntensity = baseIntensity * (1.0f + (float)math.sin(time * flickerSpeed) * flickerAmount);
                
                ArenaGlowService.UpdateGlow(glow.Name, intensity: flickeredIntensity);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating point glow: {ex.Message}");
            }
        }

        private static void UpdateLinearGlow(GlowData glow, double time, float deltaTime)
        {
            try
            {
                // Moving light effect for linear glows
                var moveSpeed = 1.0f;
                var moveAmount = 5.0f;
                var basePosition = glow.Position;
                var movedPosition = basePosition + new float3((float)math.sin(time * moveSpeed) * moveAmount, 0, 0);
                
                ArenaGlowService.UpdateGlow(glow.Name, position: movedPosition);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating linear glow: {ex.Message}");
            }
        }

        private static void UpdateAreaGlow(GlowData glow, double time, float deltaTime)
        {
            try
            {
                // Gradual radius change for area glows
                var radiusSpeed = 0.3f;
                var radiusAmount = 0.2f;
                var baseRadius = glow.Radius;
                var changedRadius = baseRadius * (1.0f + (float)math.sin(time * radiusSpeed) * radiusAmount);
                
                ArenaGlowService.UpdateGlow(glow.Name, radius: changedRadius);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowSystem] Error updating area glow: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ECS System for managing glow entity creation and destruction
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ArenaGlowEntitySystem : ISystem
    {
        private EntityQuery _glowQuery;

        public void OnCreate(ref SystemState state)
        {
            _glowQuery = state.GetEntityQuery(ComponentType.ReadOnly<GlowComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            try
            {
                if (!ArenaGlowService.Instance.IsInitialized)
                    return;

                // Create entities for new glows
                CreateGlowEntities(ref state);
                
                // Update existing glow entities
                UpdateGlowEntities(ref state);
                
                // Clean up destroyed glow entities
                CleanupDestroyedGlows(ref state);
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowEntitySystem] Error in OnUpdate: {ex.Message}");
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        private static void CreateGlowEntities(ref SystemState state)
        {
            try
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var glowNames = ArenaGlowService.GetAllGlowNames();
                
                foreach (var glowName in glowNames)
                {
                    var glowData = ArenaGlowService.GetGlow(glowName);
                    if (glowData == null || !glowData.IsActive)
                        continue;

                    // Check if entity already exists for this glow
                    var existingEntity = FindGlowEntity(ref state, glowName);
                    if (existingEntity != Entity.Null)
                        continue;

                    // Create new glow entity
                    var glowEntity = ecb.CreateEntity();
                    ecb.AddComponent(glowEntity, new GlowComponent
                    {
                        GlowName = glowName,
                        Position = glowData.Position,
                        Radius = glowData.Radius,
                        Color = glowData.Color,
                        Intensity = glowData.Intensity,
                        GlowType = glowData.Type
                    });

                    // Add transform components
                    ecb.AddComponent(glowEntity, LocalTransform.FromPositionRotationScale(
                        glowData.Position, glowData.Rotation, 1.0f));

                    // Add render components for visual representation
                    AddGlowRenderComponents(ref ecb, glowEntity, glowData);
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowEntitySystem] Error creating glow entities: {ex.Message}");
            }
        }

        private static void UpdateGlowEntities(ref SystemState state)
        {
            try
            {
                foreach (var (glow, entity) in SystemAPI.Query<RefRO<GlowComponent>>().WithEntityAccess())
                {
                    var glowData = ArenaGlowService.GetGlow(glow.ValueRO.GlowName);
                    if (glowData == null)
                        continue;

                    // Update entity position
                    if (VAuto.Core.Core.TryRead<LocalTransform>(entity, out var transform))
                    {
                        transform.Position = glowData.Position;
                        VAuto.Core.Core.Write(entity, transform);
                    }

                    // Update glow component data
                    var updatedGlow = glow.ValueRO;
                    updatedGlow.Position = glowData.Position;
                    updatedGlow.Radius = glowData.Radius;
                    updatedGlow.Color = glowData.Color;
                    updatedGlow.Intensity = glowData.Intensity;
                    
                    SystemAPI.SetComponent(entity, updatedGlow);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowEntitySystem] Error updating glow entities: {ex.Message}");
            }
        }

        private static void CleanupDestroyedGlows(ref SystemState state)
        {
            try
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                
                foreach (var (glow, entity) in SystemAPI.Query<RefRO<GlowComponent>>().WithEntityAccess())
                {
                    var glowData = ArenaGlowService.GetGlow(glow.ValueRO.GlowName);
                    if (glowData == null || !glowData.IsActive)
                    {
                        // Destroy the entity
                        ecb.DestroyEntity(entity);
                    }
                }

                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"[ArenaGlowEntitySystem] Error cleaning up glows: {ex.Message}");
            }
        }

        private static Entity FindGlowEntity(ref SystemState state, string glowName)
        {
            foreach (var (glow, entity) in SystemAPI.Query<RefRO<GlowComponent>>().WithEntityAccess())
            {
                if (glow.ValueRO.GlowName == glowName)
                    return entity;
            }
            return Entity.Null;
        }

        private static void AddGlowRenderComponents(ref EntityCommandBuffer ecb, Entity entity, GlowData glowData)
        {
            // This would add actual rendering components for the glow effect
            // For now, we'll add placeholder components
            
            // Add material component
            // ecb.AddComponent(entity, new MaterialComponent { ... });
            
            // Add particle system component
            // ecb.AddComponent(entity, new ParticleSystemComponent { ... });
            
            // Add light component if it's a light glow
            if (glowData.Type == GlowType.Point)
            {
                ecb.AddComponent(entity, new LightComponent { Color = glowData.Color, Intensity = glowData.Intensity });
            }
        }
    }

    /// <summary>
    /// Component for storing glow data on entities
    /// </summary>
    public struct GlowComponent : IComponentData
    {
        public string GlowName;
        public float3 Position;
        public float Radius;
        public float4 Color;
        public float Intensity;
        public GlowType GlowType;
    }
}