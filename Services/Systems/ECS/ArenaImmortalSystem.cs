using Unity.Entities;
using ProjectM;
using VAuto.Services.Systems;
using VAuto.Core;

namespace VAuto.Services.ECS
{
    /// <summary>
    /// ECS System that enforces immortality for players in designated arena zones.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ArenaImmortalSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var zoneService = ArenaZoneService.Instance;
            
            // Query all players with Health component
            foreach (var (health, user, entity) in SystemAPI.Query<RefRW<Health>, RefRO<User>>().WithEntityAccess())
            {
                ulong platformId = user.ValueRO.PlatformId;
                
                // Check if player is in an immortal zone
                if (zoneService.IsPlayerImmortal(platformId))
                {
                    // Prevent death and keep health high
                    if (health.ValueRO.Value < health.ValueRO.MaxHealth)
                    {
                        var h = health.ValueRW;
                        h.Value = h.MaxHealth;
                        VAuto.Core.Core.Write(entity, h);
                    }
                    
                    // Note: In V Rising, immortality is often handled by specific buffs (Invulnerable),
                    // but forcing Health value is a robust fallback for arena automation.
                }
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}