using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Mathematics;

namespace VAuto.Extensions
{
    /// <summary>
    /// JSON converter for Unity.Mathematics.float2 types
    /// Enables proper serialization and deserialization of 2D position data
    /// Used throughout arena systems for 2D coordinates and UI positioning
    /// Supports both object format {x, y} and array format [x, y]
    /// </summary>
    public class Float2Converter : JsonConverter<float2>
    {
        public override float2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                throw new JsonException($"Unexpected token type {reader.TokenType} for float2. Expected StartArray or StartObject.");
            }
        }

        public override void Write(Utf8JsonWriter writer, float2 value, JsonSerializerOptions options)
        {
            // Default to array format for consistency
            WriteAsArray(writer, value);
        }

        private float2 ReadFromArray(Utf8JsonReader reader)
        {
            reader.Read(); // Move to first number

            var x = reader.GetSingle();
            reader.Read(); // Move to second number
            var y = reader.GetSingle();

            reader.Read(); // Move to EndArray

            return new float2(x, y);
        }

        private float2 ReadFromObject(Utf8JsonReader reader)
        {
            float x = 0, y = 0;

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

            return new float2(x, y);
        }

        private void WriteAsArray(Utf8JsonWriter writer, float2 value)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteEndArray();
        }

        private void WriteAsObject(Utf8JsonWriter writer, float2 value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Converter factory for float2 types
    /// Automatically chooses appropriate converter based on context
    /// </summary>
    public class Float2ConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(float2);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(float2))
            {
                return new Float2Converter();
            }

            throw new NotSupportedException($"Cannot create converter for type {typeToConvert.Name}");
        }
    }
}












