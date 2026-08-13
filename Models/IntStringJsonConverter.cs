using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartResumeAnalyzer.Models
{
    public class IntStringJsonConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            {
                return value;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (int.TryParse(stringValue, out var parsed))
                {
                    return parsed;
                }
            }

            throw new JsonException($"Unable to convert token of type {reader.TokenType} to int.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}

