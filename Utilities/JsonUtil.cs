using System;
using System.Text.Json;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using ProjectM;

namespace VAuto.Utilities
{
    /// <summary>
    /// JSON utilities for VAuto requirements
    /// For zones, Vector2/Vector3 serialization, and snapshots
    /// </summary>
    public static class JsonUtil
    {
        private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions _extendedOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = 
            {
                new Vector2Converter(),
                new Vector3Converter(),
                new PrefabGUIDConverter(),
                new EntityConverter(),
                new AabbConverter()
            }
        };

        /// <summary>
        /// Serialize object to JSON with default options
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _defaultOptions);
        }

        /// <summary>
        /// Serialize object to JSON with extended options
        /// </summary>
        public static string SerializeExtended<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _extendedOptions);
        }

        /// <summary>
        /// Deserialize JSON to object with default options
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _defaultOptions);
        }

        /// <summary>
        /// Deserialize JSON to object with extended options
        /// </summary>
        public static T DeserializeExtended<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _extendedOptions);
        }

        /// <summary>
        /// Serialize object to file
        /// </summary>
        public static bool SerializeToFile<T>(T obj, string filePath)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(obj, _extendedOptions);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[JsonUtil] Failed to serialize to {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deserialize object from file
        /// </summary>
        public static T DeserializeFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return default(T);
                var json = File.ReadAllText(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, _extendedOptions);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[JsonUtil] Failed to deserialize from {filePath}: {ex.Message}");
                return default(T);
            }
        }
    }

    /// <summary>
    /// Vector2 JSON converter
    /// </summary>
    public class Vector2Converter : System.Text.Json.Serialization.JsonConverter<float2>
    {
        public override float2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return float2.zero;
            
            var parts = value.Split(',');
            if (parts.Length == 2 && 
                float.TryParse(parts[0].Trim(), out var x) && 
                float.TryParse(parts[1].Trim(), out var y))
            {
                return new float2(x, y);
            }
            return float2.zero;
        }

        public override void Write(Utf8JsonWriter writer, float2 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.x},{value.y}");
        }
    }

    /// <summary>
    /// Vector3 JSON converter
    /// </summary>
    public class Vector3Converter : System.Text.Json.Serialization.JsonConverter<float3>
    {
        public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return float3.zero;
            
            var parts = value.Split(',');
            if (parts.Length == 3 && 
                float.TryParse(parts[0].Trim(), out var x) && 
                float.TryParse(parts[1].Trim(), out var y) && 
                float.TryParse(parts[2].Trim(), out var z))
            {
                return new float3(x, y, z);
            }
            return float3.zero;
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.x},{value.y},{value.z}");
        }
    }

    /// <summary>
    /// PrefabGUID JSON converter
    /// </summary>
    public class PrefabGUIDConverter : System.Text.Json.Serialization.JsonConverter<PrefabGUID>
    {
        public override PrefabGUID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetInt32();
            return new PrefabGUID(value);
        }

        public override void Write(Utf8JsonWriter writer, PrefabGUID value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.GuidHash);
        }
    }

    /// <summary>
    /// Entity JSON converter
    /// </summary>
    public class EntityConverter : System.Text.Json.Serialization.JsonConverter<Entity>
    {
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return Entity.Null;
            var parts = value.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out var index) && int.TryParse(parts[1], out var version))
            {
                return new Entity { Index = index, Version = version };
            }
            return Entity.Null;
        }

        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Index},{value.Version}");
        }
    }

    /// <summary>
    /// Aabb JSON converter
    /// </summary>
    public class AabbConverter : System.Text.Json.Serialization.JsonConverter<Aabb>
    {
        public override Aabb Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return new Aabb();
            var parts = value.Split(',');
            if (parts.Length == 6 &&
                float.TryParse(parts[0], out var minx) &&
                float.TryParse(parts[1], out var miny) &&
                float.TryParse(parts[2], out var minz) &&
                float.TryParse(parts[3], out var maxx) &&
                float.TryParse(parts[4], out var maxy) &&
                float.TryParse(parts[5], out var maxz))
            {
                return new Aabb { Min = new float3(minx, miny, minz), Max = new float3(maxx, maxy, maxz) };
            }
            return new Aabb();
        }

        public override void Write(Utf8JsonWriter writer, Aabb value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Min.x},{value.Min.y},{value.Min.z},{value.Max.x},{value.Max.y},{value.Max.z}");
        }
    }
}
