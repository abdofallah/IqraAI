using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Helpers.Json
{
    public class ObjectToPrimitiveConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long l))
                    {
                        return l;
                    }
                    return reader.GetDouble();
                case JsonTokenType.String:
                    if (reader.TryGetDateTime(out DateTime datetime))
                    {
                        return datetime;
                    }
                    return reader.GetString();
                case JsonTokenType.Null:
                    return null;
                // Handle StartObject and StartArray if you expect complex objects, 
                // but for this specific use case, we are mainly concerned with primitives.
                // If you need to support them, you can use JsonElement.ParseValue(ref reader).
                default:
                    // Fallback for other types like arrays or objects
                    return JsonElement.ParseValue(ref reader).Clone();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // We don't need custom write logic for this use case.
            // The default serializer can handle writing primitives.
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
