using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace VAuto.Data.Zones
{
    /// <summary>
    /// Minimal zone data structures for JSON serialization
    /// Only for zone system requirements
    /// </summary>
    public class ZoneData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public float3 Center { get; set; }
        public float Radius { get; set; } = 50f;
        public string Type { get; set; } = "Arena";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ZoneConfig
    {
        public List<ZoneData> Zones { get; set; } = new();
        public string Version { get; set; } = "1.0.0";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Snapshot data structures
    /// </summary>
    public class PlayerSnapshot
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public float3 Position { get; set; }
        public float3 Rotation { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Inventory { get; set; } = new();
        public Dictionary<string, object> Abilities { get; set; } = new();
        public Dictionary<string, object> Equipment { get; set; } = new();
        public string ZoneId { get; set; } = string.Empty;
    }

    public class SnapshotConfig
    {
        public List<PlayerSnapshot> Snapshots { get; set; } = new();
        public string Version { get; set; } = "1.0.0";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int MaxSnapshots { get; set; } = 100;
        public bool AutoCleanup { get; set; } = true;
    }
}
