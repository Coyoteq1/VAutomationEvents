using System;
using System.Collections.Generic;
using VAuto.Data;

namespace VAuto.EventAdapters
{
    public static class SwapEventBus
    {
        public static event Action<string, string, IEnumerable<GearEntry>> SwapRequested = delegate { };
        public static event Action<string, string> SwapEnded = delegate { };

        public static void EmitSwapRequested(string playerId, string sessionId, IEnumerable<GearEntry> gear) => SwapRequested(playerId, sessionId, gear);
        public static void EmitSwapEnded(string playerId, string sessionId) => SwapEnded(playerId, sessionId);
    }
}