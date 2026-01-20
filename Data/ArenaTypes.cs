using ProjectM;
using Stunlock.Core;
using System.Collections.Generic;

namespace VAuto.Data
{
    /// <summary>
    /// Arena armor set configuration
    /// </summary>
    public class ArenaArmorSet
    {
        public string Name { get; set; } = "";
        public PrefabGUID Head { get; set; }
        public PrefabGUID Chest { get; set; }
        public PrefabGUID Legs { get; set; }
        public PrefabGUID Feet { get; set; }
        public PrefabGUID Weapon { get; set; }
        public List<PrefabGUID> Accessories { get; set; } = new();
    }

    /// <summary>
    /// Arena consumable configuration
    /// </summary>
    public class ArenaConsumable
    {
        public string Name { get; set; } = "";
        public PrefabGUID Prefab { get; set; }
        public int StackSize { get; set; } = 1;
        public float CooldownSeconds { get; set; } = 0;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Arena loadout configuration
    /// </summary>
    public class ArenaLoadout
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ArenaArmorSet ArmorSet { get; set; } = new();
        public List<ArenaConsumable> Consumables { get; set; } = new();
        public List<PrefabGUID> Abilities { get; set; } = new();
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// Defines the shape of an arena zone for visual effects
    /// </summary>
    public enum ArenaZoneShape
    {
        Circle,
        Box
    }

    /// <summary>
    /// Configuration for an arena zone and its visual boundaries
    /// </summary>
    public class ArenaZone
    {
        public string Name { get; set; } = "Default Arena";
        public Unity.Mathematics.float3 Center { get; set; }
        public ArenaZoneShape Shape { get; set; } = ArenaZoneShape.Circle;
        public float Radius { get; set; } = 10f; // For Circle
        public Unity.Mathematics.float2 Dimensions { get; set; } // For Box (Width, Height)
        public float GlowSpacing { get; set; } = 2.0f;
        public float TimerSeconds { get; set; } = 0f; // 0 = infinite
        public bool IsImmortal { get; set; } = false;
        public bool IsRepeating { get; set; } = false;
        
        public bool IsValid => Shape == ArenaZoneShape.Circle ? Radius > 0f : (Dimensions.x > 0f && Dimensions.y > 0f);
    }
}












