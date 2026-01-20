using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;

namespace VAuto.Utilities
{
    /// <summary>
    /// Enhanced float3 JSON utilities with additional helper methods
    /// Provides serialization, deserialization, and manipulation for Unity.Mathematics.float3
    /// </summary>
    public static class Float3Json
    {
        #region Serialization Methods

        /// <summary>
        /// Serialize float3 to JSON array format [x, y, z]
        /// </summary>
        public static string SerializeArray(float3 value)
        {
            return $"[{value.x}, {value.y}, {value.z}]";
        }

        /// <summary>
        /// Serialize float3 to JSON object format {"x": x, "y": y, "z": z}
        /// </summary>
        public static string SerializeObject(float3 value)
        {
            return $"{{\"x\":{value.x},\"y\":{value.y},\"z\":{value.z}}}";
        }

        /// <summary>
        /// Serialize float3 with pretty formatting
        /// </summary>
        public static string SerializePretty(float3 value)
        {
            return $"{{\n  \"x\": {value.x},\n  \"y\": {value.y},\n  \"z\": {value.z}\n}}";
        }

        /// <summary>
        /// Deserialize float3 from JSON string (auto-detects format)
        /// </summary>
        public static float3 Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return float3.zero;

            json = json.Trim();
            
            if (json.StartsWith("["))
                return DeserializeArray(json);
            else if (json.StartsWith("{"))
                return DeserializeObject(json);
            else
                throw new JsonException("Invalid float3 JSON format. Expected array [x,y,z] or object {x,y,z}");
        }

        /// <summary>
        /// Deserialize float3 from array format [x, y, z]
        /// </summary>
        public static float3 DeserializeArray(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                
                var values = JsonSerializer.Deserialize<float[]>(json, options);
                if (values?.Length >= 3)
                    return new float3(values[0], values[1], values[2]);
                
                throw new JsonException("Array must contain at least 3 elements");
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to deserialize float3 from array: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserialize float3 from object format {"x": x, "y": y, "z": z}
        /// </summary>
        public static float3 DeserializeObject(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                
                var obj = JsonSerializer.Deserialize<Float3Object>(json, options);
                return new float3(obj.X, obj.Y, obj.Z);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to deserialize float3 from object: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convert float3 to string representation
        /// </summary>
        public static string ToString(float3 value, string format = "F2")
        {
            return $"({value.x.ToString(format)}, {value.y.ToString(format)}, {value.z.ToString(format)})";
        }

        /// <summary>
        /// Parse float3 from string format "(x, y, z)"
        /// </summary>
        public static float3 Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return float3.zero;

            // Remove parentheses and split by comma
            value = value.Trim().Trim('(', ')');
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 3 && 
                float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y) &&
                float.TryParse(parts[2].Trim(), out float z))
            {
                return new float3(x, y, z);
            }

            throw new FormatException($"Invalid float3 format: {value}");
        }

        /// <summary>
        /// Try parse float3 from string, returns false if invalid
        /// </summary>
        public static bool TryParse(string value, out float3 result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                result = float3.zero;
                return false;
            }
        }

        /// <summary>
        /// Get distance between two float3 positions
        /// </summary>
        public static float Distance(float3 a, float3 b)
        {
            return math.distance(a, b);
        }

        /// <summary>
        /// Check if float3 is zero (all components are 0)
        /// </summary>
        public static bool IsZero(float3 value, float tolerance = 0.001f)
        {
            return math.abs(value.x) < tolerance && 
                   math.abs(value.y) < tolerance && 
                   math.abs(value.z) < tolerance;
        }

        /// <summary>
        /// Clamp float3 values within specified bounds
        /// </summary>
        public static float3 Clamp(float3 value, float3 min, float3 max)
        {
            return new float3(
                math.clamp(value.x, min.x, max.x),
                math.clamp(value.y, min.y, max.y),
                math.clamp(value.z, min.z, max.z)
            );
        }

        /// <summary>
        /// Linear interpolation between two float3 values
        /// </summary>
        public static float3 Lerp(float3 a, float3 b, float t)
        {
            return math.lerp(a, b, math.clamp(t, 0f, 1f));
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Serialize array of float3 values
        /// </summary>
        public static string SerializeArray(float3[] values)
        {
            if (values == null || values.Length == 0)
                return "[]";

            var result = "[";
            for (int i = 0; i < values.Length; i++)
            {
                result += SerializeArray(values[i]);
                if (i < values.Length - 1)
                    result += ",";
            }
            result += "]";
            return result;
        }

        /// <summary>
        /// Deserialize array of float3 values
        /// </summary>
        public static float3[] DeserializeFloat3Array(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                
                return JsonSerializer.Deserialize<float3[]>(json, options);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to deserialize float3 array: {ex.Message}", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for object format deserialization
    /// </summary>
    internal class Float3Object
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    /// <summary>
    /// Enhanced float3 JSON converter with additional error handling and format options
    /// </summary>
    public class EnhancedFloat3Converter : JsonConverter<float3>
    {
        public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return ReadFromArray(reader);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                return ReadFromObject(reader);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return Float3Json.Parse(stringValue);
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} for float3");
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            // Default to array format for consistency
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }

        private float3 ReadFromArray(Utf8JsonReader reader)
        {
            reader.Read(); // Move to first number
            var x = reader.GetSingle();
            reader.Read(); // Move to second number
            var y = reader.GetSingle();
            reader.Read(); // Move to third number
            var z = reader.GetSingle();
            reader.Read(); // Move to EndArray
            return new float3(x, y, z);
        }

        private float3 ReadFromObject(Utf8JsonReader reader)
        {
            float x = 0, y = 0, z = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName.ToLower())
                    {
                        case "x": x = reader.GetSingle(); break;
                        case "y": y = reader.GetSingle(); break;
                        case "z": z = reader.GetSingle(); break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            return new float3(x, y, z);
        }
    }
}
