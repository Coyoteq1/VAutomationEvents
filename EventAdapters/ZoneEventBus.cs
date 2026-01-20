using System;
using VAuto.Automation;

namespace VAuto.EventAdapters
{
    public static class ZoneEventBus
    {
        public static event Action<string, Zone> PlayerEnteredZone = delegate { };
        public static event Action<string, Zone> PlayerLeftZone = delegate { };

        public static void EmitPlayerEntered(string playerId, Zone zone) => PlayerEnteredZone(playerId, zone);
        public static void EmitPlayerLeft(string playerId, Zone zone) => PlayerLeftZone(playerId, zone);
    }
}