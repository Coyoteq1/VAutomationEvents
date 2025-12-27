using System.Collections.Generic;
using Stunlock.Core;

namespace VAuto.Data
{
    public static class ArenaBuffs
    {
        public static readonly Dictionary<string, PrefabGUID> Buffs = new()
        {
            ["ArenaSpeed"] = new PrefabGUID(1234567890),
            ["ArenaStrength"] = new PrefabGUID(1234567891),
            ["ArenaDefense"] = new PrefabGUID(1234567892),
            ["ArenaRegeneration"] = new PrefabGUID(1234567893),
            ["ArenaResistance"] = new PrefabGUID(1234567894),
            ["ArenaIntellect"] = new PrefabGUID(1234567895),
            ["ArenaDexterity"] = new PrefabGUID(1234567896),
            ["ArenaVitality"] = new PrefabGUID(1234567897),
        };

        public static bool TryGetBuff(string buffName, out PrefabGUID guid)
        {
            return Buffs.TryGetValue(buffName.ToLower(), out guid);
        }
    }
}












