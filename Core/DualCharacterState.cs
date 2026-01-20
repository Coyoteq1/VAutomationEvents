using System;
using Unity.Entities;
using Unity.Mathematics;
using Il2CppInterop.Runtime;
using VAuto.Core;
using VAuto.Services;
using System.Collections.Generic;
using System.Threading;
using ProjectM.Shared;

namespace VAuto.Core
{
    /// <summary>
    /// Marker component for PvP practice characters
    /// </summary>
    public struct PvPPracticeTag { }

    /// <summary>
    /// Marker component for frozen characters (renamed to avoid VRising conflicts)
    /// </summary>
    public struct VAutoFrozenCharacterTag { }

    /// <summary>
    /// State tracking for dual character system per player
    /// </summary>
    public class DualCharacterState
    {
        public Entity NormalCharacter;           // PlayerName (original)
        public Entity ArenaCharacter;            // PlayerNamePvP (arena character)
        public bool IsArenaActive;               // Which character is currently active
        public float3 LastNormalPosition;        // Where to return normal character
        public DateTime ArenaCreatedAt;          // When arena character was created
        public bool ArenaNeedsRespawn;           // Flag for recreation after restart
        public DateTime LastSwapTime;            // Last time characters were swapped
        public string OriginalBloodType;         // Normal character's blood type
        public string ArenaBloodType;            // Arena character's blood type (default: Rogue)
        public bool IsInitialized;               // Whether dual state is fully initialized
        
        // Legacy properties for backward compatibility
        public Entity PvPCharacter { get => ArenaCharacter; set => ArenaCharacter = value; }
        public bool IsPvPActive { get => IsArenaActive; set => IsArenaActive = value; }
        public DateTime PvPCreatedAt { get => ArenaCreatedAt; set => ArenaCreatedAt = value; }
        public bool PvPNeedsRespawn { get => ArenaNeedsRespawn; set => ArenaNeedsRespawn = value; }
    }

}
