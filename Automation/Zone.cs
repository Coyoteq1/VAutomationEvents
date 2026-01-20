using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VAuto.Automation
{
    public class Zone
    {
        [JsonProperty("zoneId")]
        public string ZoneId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("entry")]
        public Entry Entry { get; set; }

        [JsonProperty("mobs")]
        public List<Mob> Mobs { get; set; }

        [JsonProperty("boss")]
        public Boss Boss { get; set; }

        [JsonProperty("loot")]
        public Loot Loot { get; set; }

        [JsonProperty("schematic")]
        public Schematic Schematic { get; set; }

        [JsonProperty("effects")]
        public Effects Effects { get; set; }

        [JsonProperty("permissions")]
        public Permissions Permissions { get; set; }

        [JsonProperty("audit")]
        public Audit Audit { get; set; }
    }

    public class Location
    {
        [JsonProperty("center")]
        public Center Center { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("bounds")]
        public Bounds Bounds { get; set; }
    }

    public class Center
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }
    }

    public class Bounds
    {
        [JsonProperty("min")]
        public Center Min { get; set; }

        [JsonProperty("max")]
        public Center Max { get; set; }
    }

    public class Entry
    {
        [JsonProperty("onEnter")]
        public OnEnter OnEnter { get; set; }

        [JsonProperty("onExit")]
        public OnExit OnExit { get; set; }
    }

    public class OnEnter
    {
        [JsonProperty("giveGear")]
        public bool GiveGear { get; set; }

        [JsonProperty("gearList")]
        public List<Gear> GearList { get; set; }

        [JsonProperty("overrideBlood")]
        public bool OverrideBlood { get; set; }

        [JsonProperty("bloodType")]
        public string BloodType { get; set; }

        [JsonProperty("uiEffect")]
        public string UiEffect { get; set; }

        [JsonProperty("glowEffect")]
        public string GlowEffect { get; set; }

        [JsonProperty("mapEffect")]
        public string MapEffect { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class OnExit
    {
        [JsonProperty("removeGear")]
        public bool RemoveGear { get; set; }

        [JsonProperty("restoreBlood")]
        public bool RestoreBlood { get; set; }

        [JsonProperty("uiEffect")]
        public string UiEffect { get; set; }

        [JsonProperty("glowEffect")]
        public string GlowEffect { get; set; }

        [JsonProperty("mapEffect")]
        public string MapEffect { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class Gear
    {
        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("equip")]
        public bool Equip { get; set; }
    }

    public class Mob
    {
        [JsonProperty("mobName")]
        public string MobName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("spawnRate")]
        public float SpawnRate { get; set; }

        [JsonProperty("respawnInterval")]
        public int RespawnInterval { get; set; }
    }

    public class Boss
    {
        [JsonProperty("bossName")]
        public string BossName { get; set; }

        [JsonProperty("bossLevel")]
        public int BossLevel { get; set; }

        [JsonProperty("respawn")]
        public Respawn Respawn { get; set; }

        [JsonProperty("reward")]
        public List<Reward> Reward { get; set; }

        [JsonProperty("validation")]
        public Validation Validation { get; set; }
    }

    public class Respawn
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class Reward
    {
        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("chance")]
        public float Chance { get; set; }
    }

    public class Validation
    {
        [JsonProperty("requiresDevApproval")]
        public bool RequiresDevApproval { get; set; }

        [JsonProperty("requiresAdmin")]
        public bool RequiresAdmin { get; set; }
    }

    public class Loot
    {
        [JsonProperty("chests")]
        public List<Chest> Chests { get; set; }

        [JsonProperty("dropTables")]
        public List<DropTable> DropTables { get; set; }
    }

    public class Chest
    {
        [JsonProperty("position")]
        public Center Position { get; set; }

        [JsonProperty("lootTable")]
        public List<LootItem> LootTable { get; set; }
    }

    public class DropTable
    {
        [JsonProperty("tableName")]
        public string TableName { get; set; }

        [JsonProperty("items")]
        public List<LootItem> Items { get; set; }
    }

    public class LootItem
    {
        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("chance")]
        public float Chance { get; set; }
    }

    public class Schematic
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("schematicId")]
        public string SchematicId { get; set; }

        [JsonProperty("saveOnComplete")]
        public bool SaveOnComplete { get; set; }

        [JsonProperty("autoShareToLog")]
        public bool AutoShareToLog { get; set; }
    }

    public class Effects
    {
        [JsonProperty("ui")]
        public Ui Ui { get; set; }

        [JsonProperty("glow")]
        public Glow Glow { get; set; }

        [JsonProperty("map")]
        public Map Map { get; set; }
    }

    public class Ui
    {
        [JsonProperty("enterEffect")]
        public string EnterEffect { get; set; }

        [JsonProperty("exitEffect")]
        public string ExitEffect { get; set; }

        [JsonProperty("hudMessage")]
        public string HudMessage { get; set; }
    }

    public class Glow
    {
        [JsonProperty("enterGlow")]
        public string EnterGlow { get; set; }

        [JsonProperty("exitGlow")]
        public string ExitGlow { get; set; }
    }

    public class Map
    {
        [JsonProperty("enterMapEffect")]
        public string EnterMapEffect { get; set; }

        [JsonProperty("exitMapEffect")]
        public string ExitMapEffect { get; set; }
    }

    public class Permissions
    {
        [JsonProperty("requiresDevApproval")]
        public bool RequiresDevApproval { get; set; }

        [JsonProperty("requiresAdmin")]
        public bool RequiresAdmin { get; set; }

        [JsonProperty("requiresSnapshot")]
        public bool RequiresSnapshot { get; set; }
    }

    public class Audit
    {
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("logId")]
        public string LogId { get; set; }
    }
}