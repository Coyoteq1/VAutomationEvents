using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace VAuto.Data
{
    /// <summary>
    /// Zone data structure for zone management
    /// </summary>
    public class Zone
    {
        public string ZoneId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float3 Center { get; set; }
        public float Radius { get; set; } = 50f;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Gear entry for equipment management
    /// </summary>
    public class GearEntry
    {
        public string ItemName { get; set; } = string.Empty;
        public PrefabGUID ItemGuid { get; set; }
        public int Slot { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Boss entry for boss management
    /// </summary>
    public class BossEntry
    {
        public string BossId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PrefabGUID PrefabGuid { get; set; }
        public float3 Position { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime SpawnedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Zone effect result for zone operations
    /// </summary>
    public class ZoneEffectResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EffectType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Boss data structure
    /// </summary>
    public class Boss
    {
        public string Name { get; set; } = string.Empty;
        public PrefabGUID PrefabGuid { get; set; }
        public float3 Position { get; set; }
        public bool IsAlive { get; set; } = true;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Loot data structure
    /// </summary>
    public class Loot
    {
        public string ItemName { get; set; } = string.Empty;
        public PrefabGUID ItemGuid { get; set; }
        public int Amount { get; set; }
        public float Quality { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}

namespace VAuto.Automation
{
    /// <summary>
    /// Zone class in Automation namespace
    /// </summary>
    public class Zone
    {
        public string ZoneId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float3 Center { get; set; }
        public float Radius { get; set; } = 50f;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
