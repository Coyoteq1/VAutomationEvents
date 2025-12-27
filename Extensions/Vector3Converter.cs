using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;

namespace VAuto.Extensions
{
    /// <summary>
    /// JSON converter for Unity.Mathematics.float3 types
    /// Enables proper serialization and deserialization of position data
    /// Used throughout arena lifecycle for position tracking and logging
    /// Supports both object format {x, y, z} and array format [x, y, z]
    /// </summary>
    public class Float3Converter : JsonConverter<float3>
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
            else
            {
                throw new JsonException($"Unexpected token type {reader.TokenType} for float3. Expected StartArray or StartObject.");
            }
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            // Default to array format for consistency
            WriteAsArray(writer, value);
        }

        private float3 ReadFromArray(Utf8JsonReader reader)
        {
            reader.Read(); // Move to first number

            var result = new float3();
            result.x = reader.GetSingle();
            reader.Read(); // Move to second number
            result.y = reader.GetSingle();
            reader.Read(); // Move to third number
            result.z = reader.GetSingle();

            reader.Read(); // Move to EndArray

            return result;
        }

        private float3 ReadFromObject(Utf8JsonReader reader)
        {
            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();

                    reader.Read(); // Move to property value

                    switch (propertyName.ToLower())
                    {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
            }

            return new float3(x, y, z);
        }

        private void WriteAsArray(Utf8JsonWriter writer, float3 value)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }

        private void WriteAsObject(Utf8JsonWriter writer, float3 value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Converter factory for float3 types
    /// Automatically chooses appropriate converter based on context
    /// </summary>
    public class Float3ConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(float3);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(float3))
            {
                return new Float3Converter();
            }

            throw new NotSupportedException($"Cannot create converter for type {typeToConvert.Name}");
        }
    }
}












