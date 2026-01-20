using VampireCommandFramework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VAuto.Core;
using VAuto.Services.Systems;
using ProjectM;
using System;

namespace VAuto.Commands.Arena
{
    [CommandGroup("arena")]
    public static class ArenaBuildCommands
    {
        [Command("tile add", adminOnly: true, usage: ".arena tile add <prefab_name>")]
        public static void AddTileCommand(ChatCommandContext ctx, string prefabName)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                if (!VRCore.EM.TryGetComponentData(character, out Translation translation))
                {
                    ctx.Reply("Could not determine your position.");
                    return;
                }

                float3 pos = translation.Value;
                
                // Placeholder for spawning logic
                // In a real implementation, we would look up the prefab and spawn it
                ctx.Reply($"Attempting to add tile '{prefabName}' at {pos}...");

                // For now, we'll create a dummy entity to represent the tile in the service
                var tileEntity = VRCore.EM.CreateEntity();
                VRCore.EM.AddComponentData(tileEntity, new Translation { Value = pos });
                
                if (ArenaObjectService.Instance.AddObjectToArena(tileEntity, "default_arena", ArenaObjectService.ArenaObjectType.Structure, prefabName))
                {
                    ctx.Reply($"Successfully added tile '{prefabName}' to arena tracking.");
                }
                else
                {
                    ctx.Reply("Failed to add tile to arena tracking.");
                    VRCore.EM.DestroyEntity(tileEntity);
                }
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        [Command("glow circle", adminOnly: true, usage: ".arena glow circle <radius> <spacing> [timer] [immortal] [repeating]")]
        public static void AddGlowCircleCommand(ChatCommandContext ctx, float radius = 10f, float spacing = 2f, float timer = 0f, bool immortal = false, bool repeating = false)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                if (!VRCore.EM.TryGetComponentData(character, out Translation translation))
                {
                    ctx.Reply("Could not determine your position.");
                    return;
                }

                var zone = new VAuto.Data.ArenaZone
                {
                    Name = $"circle_{DateTime.UtcNow.Ticks}",
                    Center = translation.Value,
                    Shape = VAuto.Data.ArenaZoneShape.Circle,
                    Radius = radius,
                    GlowSpacing = spacing,
                    TimerSeconds = timer,
                    IsImmortal = immortal,
                    IsRepeating = repeating
                };

                ArenaGlowService.Instance.SpawnZoneGlow(zone, default); // TODO: Fix ArenaGlowPrefabs.Blue reference
                ctx.Reply($"Spawned circular glow at {translation.Value} (Radius: {radius}, Timer: {timer}s, Immortal: {immortal}, Repeating: {repeating}).");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        [Command("glow box", adminOnly: true, usage: ".arena glow box <width> <height> <spacing> [timer] [immortal] [repeating]")]
        public static void AddGlowBoxCommand(ChatCommandContext ctx, float width = 20f, float height = 20f, float spacing = 2f, float timer = 0f, bool immortal = false, bool repeating = false)
        {
            try
            {
                var character = ctx.Event.SenderCharacterEntity;
                if (!VRCore.EM.TryGetComponentData(character, out Translation translation))
                {
                    ctx.Reply("Could not determine your position.");
                    return;
                }

                var zone = new VAuto.Data.ArenaZone
                {
                    Name = $"box_{DateTime.UtcNow.Ticks}",
                    Center = translation.Value,
                    Shape = VAuto.Data.ArenaZoneShape.Box,
                    Dimensions = new float2(width, height),
                    GlowSpacing = spacing,
                    TimerSeconds = timer,
                    IsImmortal = immortal,
                    IsRepeating = repeating
                };

                ArenaGlowService.Instance.SpawnZoneGlow(zone, default); // TODO: Fix ArenaGlowPrefabs.Red reference
                ctx.Reply($"Spawned box glow at {translation.Value} (Size: {width}x{height}, Timer: {timer}s, Immortal: {immortal}, Repeating: {repeating}).");
            }
            catch (Exception ex)
            {
                ctx.Reply($"Error: {ex.Message}");
            }
        }

        [Command("glow clear", adminOnly: true, usage: ".arena glow clear")]
        public static void ClearGlowCommand(ChatCommandContext ctx)
        {
            ArenaGlowService.Instance.ClearAllGlows();
            ctx.Reply("Cleared all arena glow effects.");
        }
    }
}