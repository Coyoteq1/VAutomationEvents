using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;
using ProjectM;
using Stunlock.Core;

namespace VAuto.Utilities
{
    /// <summary>
    /// JSON converters for Unity types based on KindredSchematics
    /// </summary>
    public class AabbConverter : JsonConverter<Aabb>
    {
        public override Aabb Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Aabb should be an object");
            }
            reader.Read();

            var max = float3.zero;
            var min = float3.zero;
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name");
                }
                var propName = reader.GetString();
                reader.Read();
                
                switch (propName)
                {
                    case "Max":
                        max = ReadFloat3(ref reader);
                        break;
                    case "Min":
                        min = ReadFloat3(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            reader.Read();
            return new Aabb { Min = min, Max = max };
        }

        public override void Write(Utf8JsonWriter writer, Aabb value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Min");
            WriteFloat3(writer, value.Min);
            writer.WritePropertyName("Max");
            WriteFloat3(writer, value.Max);
            writer.WriteEndObject();
        }

        private static float3 ReadFloat3(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("float3 should be an object");
            }
            reader.Read();
            
            var result = float3.zero;
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name");
                }
                var propName = reader.GetString();
                reader.Read();
                
                switch (propName)
                {
                    case "x":
                        result.x = reader.GetSingle();
                        break;
                    case "y":
                        result.y = reader.GetSingle();
                        break;
                    case "z":
                        result.z = reader.GetSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            reader.Read();
            return result;
        }

        private static void WriteFloat3(Utf8JsonWriter writer, float3 value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteNumberValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteNumberValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteNumberValue(value.z);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Enhanced float3 converter for better JSON serialization
    /// </summary>
    public class SchematicFloat3Converter : JsonConverter<float3>
    {
        public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                var parts = str.Split(',');
                if (parts.Length == 3)
                {
                    return new float3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }
            }
            return ReadFloat3Internal(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.x},{value.y},{value.z}");
        }

        private static float3 ReadFloat3Internal(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("float3 should be an object");
            }
            reader.Read();
            
            var result = float3.zero;
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name");
                }
                var propName = reader.GetString();
                reader.Read();
                
                switch (propName)
                {
                    case "x":
                        result.x = reader.GetSingle();
                        break;
                    case "y":
                        result.y = reader.GetSingle();
                        break;
                    case "z":
                        result.z = reader.GetSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            reader.Read();
            return result;
        }
    }

    /// <summary>
    /// Quaternion converter for Unity quaternion serialization
    /// Handles conversion between quaternion and euler angles
    /// </summary>
    public class UnityQuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Quaternion should be an array");
            }

            var euler = new Vector3();
            reader.Read();
            euler.x = reader.GetSingle();
            reader.Read();
            euler.y = reader.GetSingle();
            reader.Read();
            euler.z = reader.GetSingle();
            reader.Read();

            return Quaternion.Euler(euler);
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
        {
            var euler = value.eulerAngles;
            writer.WriteStartArray();
            writer.WriteNumberValue(Mathf.Abs(euler.x) <= float.Epsilon ? 0f : euler.x);
            writer.WriteNumberValue(Mathf.Abs(euler.y) <= float.Epsilon ? 0f : euler.y);
            writer.WriteNumberValue(Mathf.Abs(euler.z) <= float.Epsilon ? 0f : euler.z);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// PrefabGUID converter for Unity prefab GUIDs
    /// Handles conversion between prefab names and GUID values
    /// </summary>
    public class PrefabGUIDConverter : JsonConverter<PrefabGUID>
    {
        public override PrefabGUID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("PrefabGUID should be an object");
            }

            if (reader.TokenType == JsonTokenType.Null)
                return PrefabGUID.Empty;

            var prefabName = reader.GetString();
            
            // Simple GUID mapping - expand as needed
            var guidMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Castle_Basic_T1_C"] = 123456789,
                ["Sword_Iron_T1"] = 987654321,
                ["Staff_Wood_T1"] = 55566777
                // Add more mappings as needed
            };

            if (!guidMap.TryGetValue(prefabName, out var guidValue))
            {
                Plugin.Logger?.LogWarning($"Couldn't find prefab GUID for: {prefabName}");
                return new PrefabGUID(0);
            }

            return new PrefabGUID(guidValue);
        }

        public override void Write(Utf8JsonWriter writer, PrefabGUID value, JsonSerializerOptions options)
        {
            // Write GUID as string for readability
            var guidLookup = new Dictionary<int, string>
            {
                [123456789] = "Castle_Basic_T1_C",
                [987654321] = "Sword_Iron_T1",
                [55566777] = "Staff_Wood_T1"
                // Add reverse mappings as needed
            };

            var prefabName = guidLookup.TryGetValue(value.GuidHash, out var name) ? name : "Unknown";
            writer.WriteStringValue(prefabName);
        }
    }
}
