using System;
using VAuto.Automation;
using VAuto.Data;

namespace VAuto.EventAdapters
{
    public static class BossEventBus
    {
        public static event Action<string, Zone, BossEntry> BossSpawned = delegate { };
        public static event Action<string, Zone, BossEntry> BossDefeated = delegate { };

        public static void EmitBossSpawned(string playerId, Zone zone, BossEntry boss) => BossSpawned(playerId, zone, boss);
        public static void EmitBossDefeated(string playerId, Zone zone, BossEntry boss) => BossDefeated(playerId, zone, boss);
    }
}