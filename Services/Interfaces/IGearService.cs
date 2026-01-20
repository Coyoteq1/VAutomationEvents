using System.Collections.Generic;
using VAuto.Data;

namespace VAuto.Services.Interfaces
{
    public interface IGearService
    {
        // Enter-zone opt-in
        bool IsAutoEquipEnabledForPlayer(string playerId);
        void SetAutoEquipForPlayer(string playerId, bool enabled);

        // Equip immediate gear (used by zone entry)
        bool EquipGearForPlayer(string playerId, IEnumerable<GearEntry> gearList);

        // Swap flow: start and end named swap sessions (boss/manual)
        bool StartSwapSession(string playerId, string sessionId, IEnumerable<GearEntry> swapGear);
        bool EndSwapSession(string playerId, string sessionId);

        // Revert everything (ends swap sessions first, then global snapshot)
        void RevertPlayerGear(string playerId);

        // Query active swap sessions for a player
        IEnumerable<string> GetActiveSwapSessions(string playerId);
    }
}