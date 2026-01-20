using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectM;
using Unity.Mathematics;
using VAuto.Core;
using Stunlock.Core;

namespace VAuto.Utilities
{
    /// <summary>
    /// JsonUtil is a utility class in VAMP that provides methods and converters for JSON serialization and deserialization operations.
    /// It includes custom converters for special data types and default serialization options.
    /// </summary>
    public static class JsonUtil
    {
        #region Default Serializer Options
        
        /// <summary>
        /// Default JSON serializer options with pretty printing
        /// </summary>
        public static readonly JsonSerializerOptions PrettyJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = 
            {
                new LongShortNamesConverter(),
                new UnityQuaternionConverter(),
                new AabbConverter(),
                new PrefabGUIDConverter(),
                new TimeOnlyConverter(),
                new TimeOnlyHourMinuteConverter(),
                new DayOfWeekConverter(),
                new EnhancedFloat3Converter(),
                new Float2Converter(),
                new EntityConverter()
            }
        };

        /// <summary>
        /// Compact JSON options (no pretty printing)
        /// </summary>
        public static readonly JsonSerializerOptions CompactJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = 
            {
                new LongShortNamesConverter(),
                new UnityQuaternionConverter(),
                new AabbConverter(),
                new PrefabGUIDConverter(),
                new TimeOnlyConverter(),
                new TimeOnlyHourMinuteConverter(),
                new DayOfWeekConverter(),
                new EnhancedFloat3Converter(),
                new Float2Converter(),
                new EntityConverter()
            }
        };

        /// <summary>
        /// Schematic JSON options (matches KindredSchematics format)
        /// </summary>
        public static readonly JsonSerializerOptions SchematicJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = 
            {
                new SchematicFloat3Converter(),
                new AabbConverter(),
                new UnityQuaternionConverter(),
                new PrefabGUIDConverter(),
                new EntityConverter()
            }
        };

        /// <summary>
        /// Snapshot JSON options (for player snapshot serialization)
        /// </summary>
        public static readonly JsonSerializerOptions SnapshotJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = 
            {
                new LongShortNamesConverter(),
                new UnityQuaternionConverter(),
                new AabbConverter(),
                new PrefabGUIDConverter(),
                new TimeOnlyConverter(),
                new TimeOnlyHourMinuteConverter(),
                new DayOfWeekConverter(),
                new EnhancedFloat3Converter(),
                new Float2Converter(),
                new EntityConverter()
            }
        };

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Serialize an object with pretty printing
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, PrettyJsonOptions);
        }

        /// <summary>
        /// Serialize an object with custom options
        /// </summary>
        public static string Serialize<T>(T obj, JsonSerializerOptions options)
        {
            if (options == null)
            {
                Plugin.Logger?.LogWarning("[JsonUtil] Null JsonSerializerOptions provided, using defaults");
                return JsonSerializer.Serialize(obj, PrettyJsonOptions);
            }

            if (options.Converters.Count == 0)
            {
                Plugin.Logger?.LogWarning("[JsonUtil] JsonSerializerOptions with no converters may fail to serialize complex types");
            }

            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Serialize an object with comments header
        /// </summary>
        public static string SerializeWithComments<T>(T obj, string commentHeader)
        {
            var json = JsonSerializer.Serialize(obj, PrettyJsonOptions);
            
            if (!string.IsNullOrEmpty(commentHeader))
            {
                // Add comment header at the beginning
                var commentLines = commentHeader.Split('\n');
                var commentedHeader = "// " + string.Join("\n// ", commentLines) + "\n\n";
                return commentedHeader + json;
            }
            
            return json;
        }

        /// <summary>
        /// Deserialize JSON with default options
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, PrettyJsonOptions);
        }

        /// <summary>
        /// Deserialize JSON with custom options
        /// </summary>
        public static T Deserialize<T>(string json, JsonSerializerOptions options)
        {
            if (options == null)
            {
                Plugin.Logger?.LogWarning("[JsonUtil] Null JsonSerializerOptions provided, using defaults");
                return JsonSerializer.Deserialize<T>(json, PrettyJsonOptions);
            }

            if (options.Converters.Count == 0)
            {
                Plugin.Logger?.LogWarning("[JsonUtil] JsonSerializerOptions with no converters may fail to deserialize complex types");
            }

            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Load JSON from file with automatic comment handling
        /// </summary>
        public static T LoadFromFile<T>(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return default(T);

                var json = System.IO.File.ReadAllText(filePath);
                return Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[JsonUtil] Error loading from {filePath}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Save object to JSON file with optional comments
        /// </summary>
        public static bool SaveToFile<T>(T obj, string filePath, string commentHeader = null)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                var json = string.IsNullOrEmpty(commentHeader) 
                    ? Serialize(obj) 
                    : SerializeWithComments(obj, commentHeader);

                System.IO.File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[JsonUtil] Error saving to {filePath}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Custom Converters

        /// <summary>
        /// Custom converter for long/short name tuples
        /// </summary>
        public class LongShortNamesConverter : JsonConverter<(string Long, string Short)>
        {
            public override (string Long, string Short) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var longName = "";
                    var shortName = "";

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            break;

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString();
                            reader.Read();

                            if (propertyName.Equals("Long", StringComparison.OrdinalIgnoreCase))
                                longName = reader.GetString();
                            else if (propertyName.Equals("Short", StringComparison.OrdinalIgnoreCase))
                                shortName = reader.GetString();
                        }
                    }

                    return (longName ?? "", shortName ?? "");
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read();
                    var longName = reader.GetString();
                    reader.Read();
                    var shortName = reader.GetString();
                    reader.Read(); // Skip EndArray

                    return (longName ?? "", shortName ?? "");
                }

                throw new JsonException("Invalid format for LongShortNames tuple");
            }

            public override void Write(Utf8JsonWriter writer, (string Long, string Short) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("Long", value.Long);
                writer.WriteString("Short", value.Short);
                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Custom converter for PrefabGUID objects
        /// </summary>
        public class PrefabGUIDConverter : JsonConverter<PrefabGUID>
        {
            public override PrefabGUID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    return new PrefabGUID(reader.GetInt32());
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (int.TryParse(value, out var guidValue))
                    {
                        return new PrefabGUID(guidValue);
                    }
                    throw new JsonException($"Invalid PrefabGUID format: {value}");
                }

                throw new JsonException("Invalid PrefabGUID format");
            }

            public override void Write(Utf8JsonWriter writer, PrefabGUID value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.GuidHash);
            }
        }

        /// <summary>
        /// Custom converter for TimeOnly with hours, minutes, and seconds
        /// </summary>
        public class TimeOnlyConverter : JsonConverter<TimeOnly>
        {
            public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (TimeOnly.TryParse(value, out var time))
                        return time;
                }
                throw new JsonException("Invalid TimeOnly format");
            }

            public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("HH:mm:ss"));
            }
        }

        /// <summary>
        /// Custom converter for TimeOnly with just hours and minutes
        /// </summary>
        public class TimeOnlyHourMinuteConverter : JsonConverter<TimeOnly>
        {
            public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (TimeOnly.TryParse(value, out var time))
                        return time;
                }
                throw new JsonException("Invalid TimeOnly format");
            }

            public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("HH:mm"));
            }
        }

        /// <summary>
        /// Custom converter for DayOfWeek with null support
        /// </summary>
        public class DayOfWeekConverter : JsonConverter<DayOfWeek?>
        {
            public override DayOfWeek? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (string.Equals(value, "Daily", StringComparison.OrdinalIgnoreCase))
                        return null;
                    if (Enum.TryParse<DayOfWeek>(value, true, out var dayOfWeek))
                        return dayOfWeek;
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }
                throw new JsonException("Invalid DayOfWeek format");
            }

            public override void Write(Utf8JsonWriter writer, DayOfWeek? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    writer.WriteStringValue(value.Value.ToString());
                else
                    writer.WriteStringValue("Daily");
            }
        }

        /// <summary>
        /// Custom converter for Unity.Entities.Entity type
        /// Serializes Entity as {Index, Version} for debugging and persistence
        /// </summary>
        public class EntityConverter : JsonConverter<Entity>
        {
            public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    int index = 0;
                    int version = 0;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            break;

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString();
                            reader.Read();

                            if (propertyName.Equals("Index", StringComparison.OrdinalIgnoreCase))
                                index = reader.GetInt32();
                            else if (propertyName.Equals("Version", StringComparison.OrdinalIgnoreCase))
                                version = reader.GetInt32();
                        }
                    }

                    return new Entity { Index = index, Version = version };
                }

                throw new JsonException("Invalid Entity format");
            }

            public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("Index", value.Index);
                writer.WriteNumber("Version", value.Version);
                writer.WriteEndObject();
            }
        }

        #endregion
    }
}
