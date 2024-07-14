using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using IqraCore.Attributes;

namespace ProjectIqraFrontend.Middlewares
{
    public class EndpointAwareJsonConverter : JsonConverter<object>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<(Type, string), PropertyInfo[]> _propertyCache = new ConcurrentDictionary<(Type, string), PropertyInfo[]>();

        public EndpointAwareJsonConverter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override bool CanConvert(Type typeToConvert) =>
            !typeToConvert.IsPrimitive &&
            typeToConvert != typeof(string) &&
            typeToConvert != typeof(DateTime) &&
            !typeof(IEnumerable).IsAssignableFrom(typeToConvert);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("JSON token is not a start object");
            }

            var instance = Activator.CreateInstance(typeToConvert);
            var properties = typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return instance;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("JSON token is not a property name");
                }

                string propertyName = reader.GetString() ?? throw new JsonException("Property name is null");
                reader.Read();

                if (properties.TryGetValue(propertyName, out PropertyInfo? property))
                {
                    object? value;
                    if (property.PropertyType == typeof(int))
                    {
                        value = DeserializeInt(ref reader);
                    }
                    else if (property.PropertyType == typeof(long))
                    {
                        value = DeserializeLong(ref reader);
                    }
                    else if (property.PropertyType == typeof(float))
                    {
                        value = DeserializeFloat(ref reader);
                    }
                    else if (property.PropertyType == typeof(double))
                    {
                        value = DeserializeDouble(ref reader);
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        value = DeserializeEnum(ref reader, property.PropertyType);
                    }
                    else if (property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTime))
                    {
                        value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType.IsGenericType)
                    {
                        var listType = typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]);
                        value = JsonSerializer.Deserialize(ref reader, listType, options);
                    }
                    else
                    {
                        value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    }

                    property.SetValue(instance, value);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("JSON object is incomplete");
        }

        private int DeserializeInt(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (int.TryParse(reader.GetString(), out int result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize int value");
        }

        private long DeserializeLong(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (long.TryParse(reader.GetString(), out long result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize long value");
        }

        private float DeserializeFloat(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetSingle();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (float.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize float value");
        }

        private double DeserializeDouble(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize double value");
        }

        private object DeserializeEnum(ref Utf8JsonReader reader, Type enumType)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return Enum.ToObject(enumType, reader.GetInt32());
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse(enumType, reader.GetString() ?? string.Empty);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read(); // Move to the first property
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "value")
                {
                    reader.Read(); // Move to the value
                    int enumValue = reader.GetInt32();
                    reader.Read(); // Move to the next property or end object
                    return Enum.ToObject(enumType, enumValue);
                }
            }
            throw new JsonException($"Unable to deserialize enum of type {enumType.Name}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value is string stringValue)
            {
                writer.WriteStringValue(stringValue);
                return;
            }

            if (value is int intValue)
            {
                writer.WriteNumberValue(intValue);
                return;
            }

            if (value is long longValue)
            {
                writer.WriteNumberValue(longValue);
                return;
            }

            if (value is double doubleValue)
            {
                writer.WriteNumberValue(doubleValue);
                return;
            }

            if (value is float floatValue)
            {
                writer.WriteNumberValue(floatValue);
                return;
            }

            if (value is bool boolValue)
            {
                writer.WriteBooleanValue(boolValue);
                return;
            }

            if (value is DateTime dateTimeValue)
            {
                JsonSerializer.Serialize(writer, dateTimeValue, options);
                return;
            }  

            writer.WriteStartObject();

            var type = value.GetType();
            var currentEndpoint = _httpContextAccessor.HttpContext?.Request.Path.Value ?? string.Empty;
            var properties = GetSerializableProperties(type, currentEndpoint);

            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(value);
                var propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;

                writer.WritePropertyName(propertyName);

                if (propertyValue is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (underlyingType.IsEnum)
                    {
                        SerializeEnum(writer, propertyValue);
                    }
                    else if (underlyingType.IsPrimitive)
                    {
                        JsonSerializer.Serialize(writer, propertyValue, underlyingType, options);
                    }
                    else if (propertyValue is IEnumerable enumerable)
                    {
                        writer.WriteStartArray();
                        foreach (var item in enumerable)
                        {
                            JsonSerializer.Serialize(writer, item, options);
                        }
                        writer.WriteEndArray();
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, propertyValue, options);
                    }
                }
            }

            writer.WriteEndObject();
        }

        private void SerializeInt(Utf8JsonWriter writer, int value)
        {
            writer.WriteNumberValue(value);
        }

        private void SerializeLong(Utf8JsonWriter writer, long value)
        {
            writer.WriteNumberValue(value);
        }

        private void SerializeFloat(Utf8JsonWriter writer, float value)
        {
            writer.WriteNumberValue(value);
        }

        private void SerializeDouble(Utf8JsonWriter writer, double value)
        {
            writer.WriteNumberValue(value);
        }

        private void SerializeEnum(Utf8JsonWriter writer, object enumValue)
        {
            writer.WriteStartObject();
            writer.WriteNumber("value", Convert.ToInt32(enumValue));
            writer.WriteString("name", Enum.GetName(enumValue.GetType(), enumValue));
            writer.WriteEndObject();
        }

        private PropertyInfo[] GetSerializableProperties(Type type, string currentEndpoint)
        {
            return _propertyCache.GetOrAdd((type, currentEndpoint), key =>
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => ShouldSerializeProperty(p, currentEndpoint) && !p.GetIndexParameters().Any())
                    .ToArray();
            });
        }

        private static bool ShouldSerializeProperty(PropertyInfo property, string currentEndpoint)
        {
            if (property.GetCustomAttribute<ExcludeInAllEndpointsAttribute>() is object)
            {
                return property.GetCustomAttributes<IncludeInEndpointAttribute>()
                    .Any(attr => attr.Endpoint == currentEndpoint);
            }

            return !property.GetCustomAttributes<ExcludeInEndpointAttribute>()
                .Any(attr => attr.Endpoint == currentEndpoint);
        }
    }
}