using IqraCore.Attributes;
using IqraCore.Entities.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectIqraFrontend.Middlewares
{
    public class EndpointAwareJsonConverter : JsonConverter<object>
    {
        private IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<(Type, string), PropertyInfo[]> _propertyCache = new ConcurrentDictionary<(Type, string), PropertyInfo[]>();

        public EndpointAwareJsonConverter()
        {
        }

        public void SetHttpContextAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

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
            var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite) continue;

                var jsonPropertyNameAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                string dictionaryKey;

                if (jsonPropertyNameAttr != null)
                {
                    dictionaryKey = jsonPropertyNameAttr.Name;
                }
                else
                {
                    dictionaryKey = property.Name;
                }

                properties[dictionaryKey] = property;
            }

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
                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    var value = ReadObject(ref reader, underlyingType, options);

                    try
                    {
                        var convertedValue = (value != null) ? Convert.ChangeType(value, underlyingType) : null;
                        property.SetValue(instance, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonException($"Failed to set value for property '{property.Name}'. Incoming value might be of the wrong type.", ex);
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            if (reader.IsFinalBlock && (reader.TokenType == JsonTokenType.EndObject || reader.TokenType == JsonTokenType.None))
            {
                return instance;
            }

            throw new JsonException("JSON object is incomplete");
        }

        private object? ReadObject(ref Utf8JsonReader reader, Type underlyingType, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            object? value;
            if (underlyingType == typeof(int))
            {
                value = DeserializeInt(ref reader);
            }
            else if (underlyingType == typeof(long))
            {
                value = DeserializeLong(ref reader);
            }
            else if (underlyingType == typeof(float))
            {
                value = DeserializeFloat(ref reader);
            }
            else if (underlyingType == typeof(double))
            {
                value = DeserializeDouble(ref reader);
            }
            else if (underlyingType.IsEnum)
            {
                value = DeserializeEnum(ref reader, underlyingType);
            }
            else if (underlyingType == typeof(string))
            {
                value = DeserializeString(ref reader);
            }
            else if (underlyingType == typeof(DateTime))
            {
                value = DeserializeDateTime(ref reader);
            }
            else if (underlyingType == typeof(decimal))
            {
                value = DeserializeDecimal(ref reader);
            }
            else if (underlyingType == typeof(bool))
            {
                value = DeserializeBool(ref reader);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType.IsGenericType)
            {
                var listType = typeof(List<>).MakeGenericType(underlyingType.GetGenericArguments()[0]);
                var listItemType = Nullable.GetUnderlyingType(underlyingType.GetGenericArguments()[0]) ?? underlyingType.GetGenericArguments()[0];

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("JSON token is not a start array");
                }
                reader.Read();

                value = Activator.CreateInstance(listType);

                while (true)
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        reader.Read();
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        ((IList)value).Add(null);
                        reader.Read();
                        continue;
                    }

                    var item = ReadObject(ref reader, listItemType, options);
                    ((IList)value).Add(item);
                    reader.Read();
                }

                reader.Read();
            }
            else
            {
                value = JsonSerializer.Deserialize(ref reader, underlyingType, options);
            }

            return value;
        }

        private bool DeserializeBool(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }
            else if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }
            throw new JsonException("Unable to deserialize bool value");
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

        private decimal DeserializeDecimal(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (decimal.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize decimal value");
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

        private object DeserializeString(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            throw new JsonException("Unable to deserialize string value");
        }

        private object DeserializeDateTime(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (DateTime.TryParse(reader.GetString(), out DateTime result))
                {
                    return result;
                }
            }
            throw new JsonException("Unable to deserialize DateTime value");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (WriteNoDepthProperties(writer, value, value.GetType(), options))
            {
                return;
            }

            writer.WriteStartObject();

            var type = value.GetType();
            var currentEndpoint = GetRouteTemplate();
            var properties = GetSerializableProperties(type, currentEndpoint);

            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(value);

                var propertyNameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (propertyNameAttribute != null)
                {
                    writer.WritePropertyName(propertyNameAttribute.Name);
                }
                else
                {
                    string? propertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                    writer.WritePropertyName(propertyName);
                }              

                if (!WriteNoDepthProperties(writer, propertyValue, property.PropertyType, options, property))
                {
                    JsonSerializer.Serialize(writer, propertyValue, options);
                }
            }

            writer.WriteEndObject();
        }

        private bool WriteNoDepthProperties(Utf8JsonWriter writer, object? value, Type valueType, JsonSerializerOptions options, PropertyInfo? property = null)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return true;
            }

            var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (underlyingType == typeof(string))
            {
                writer.WriteStringValue((string)value);
                return true;
            }

            if (underlyingType == typeof(int))
            {
                writer.WriteNumberValue((int)value);
                return true;
            }

            if (underlyingType == typeof(long))
            {
                writer.WriteNumberValue((long)value);
                return true;
            }

            if (underlyingType == typeof(double))
            {
                writer.WriteNumberValue((double)value);
                return true;
            }

            if (underlyingType == typeof(decimal))
            {
                writer.WriteNumberValue((decimal)value);
                return true;
            }

            if (underlyingType == typeof(float))
            {
                writer.WriteNumberValue((float)value);
                return true;
            }

            if (underlyingType == typeof(bool))
            {
                writer.WriteBooleanValue((bool)value);
                return true;
            }

            if (underlyingType == typeof(TimeOnly))
            {
                var timeOnly = (TimeOnly)value;
                writer.WriteStringValue(timeOnly.ToString("HH:mm", CultureInfo.InvariantCulture));
                return true;
            }

            if (underlyingType == typeof(DateTime))
            {
                JsonSerializer.Serialize(writer, (DateTime)value, underlyingType, options);
                return true;
            }         

            if (underlyingType.IsEnum)
            {
                if (IsApiRequest())
                {
                    writer.WriteNumberValue(Convert.ToInt32(value));
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("value", Convert.ToInt32(value));
                    writer.WriteString("name", Enum.GetName(value.GetType(), value));
                    writer.WriteEndObject();
                }
                return true;
            }

            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(DictionaryStringEnumValue<,,>))
            {
                writer.WriteStartObject();

                if (value is IInternalDictionaryProvider dictionaryProvider)
                {
                    var dictionary = dictionaryProvider.GetInternalDictionary();
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var enumType = valueType.GenericTypeArguments[1];
                        if (Enum.TryParse(enumType, entry.Key.ToString(), out object? enumValue))
                        {
                            writer.WritePropertyName(Convert.ToInt32(enumValue).ToString());
                            JsonSerializer.Serialize(writer, entry.Value, options);
                        }
                    }
                }

                writer.WriteEndObject();
                return true;
            }

            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>) && underlyingType.GetGenericArguments()[0] == typeof((TimeOnly, TimeOnly)))
            {
                writer.WriteStartArray();
                var timeRanges = (List<(TimeOnly, TimeOnly)>)value;

                foreach (var (start, end) in timeRanges)
                {
                    writer.WriteStartArray();
                    JsonSerializer.Serialize(writer, start, options);
                    JsonSerializer.Serialize(writer, end, options);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
                return true;
            }

            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
            {
                writer.WriteStartArray();
                foreach (var item in (IEnumerable)value)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }
                writer.WriteEndArray();
                return true;
            }

            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                writer.WriteStartObject();

                var keepOriginalCase = false;
                if (property != null)
                {
                    keepOriginalCase = property?.GetCustomAttribute<KeepOriginalDictionaryKeyCaseAttribute>() != null || IsApiRequest();
                }

                foreach (var entry in (IDictionary)value)
                {
                    var kvp = (DictionaryEntry)entry;

                    var propertyName = kvp.Key.ToString() ?? string.Empty;
                    if (!keepOriginalCase)
                    {
                        propertyName = options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
                    }

                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, kvp.Value, options);
                }

                writer.WriteEndObject();
                return true;
            }

            if (underlyingType.IsPrimitive)
            {
                JsonSerializer.Serialize(writer, value, underlyingType, options);
                return true;
            }
            
            return false;
        }

        private PropertyInfo[] GetSerializableProperties(Type type, string currentEndpoint)
        {
            return _propertyCache.GetOrAdd((type, currentEndpoint), key =>
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var serializableProperties = new List<PropertyInfo>();
                foreach(var property in properties)
                {
                    var isSerializable = ShouldSerializeProperty(property, currentEndpoint);

                    if (isSerializable)
                    {
                        serializableProperties.Add(property);
                    }
                }

                return serializableProperties.ToArray();
            });
        }

        private static bool ShouldSerializeProperty(PropertyInfo property, string currentEndpoint)
        {
            if (property.GetCustomAttribute<ExcludeInAllEndpointsAttribute>() is object)
            {
                return property.GetCustomAttributes<IncludeInEndpointAttribute>()
                    .Any(attr => MatchPattern(attr.Endpoint, currentEndpoint));
            }

            return !property.GetCustomAttributes<ExcludeInEndpointAttribute>()
                .Any(attr => MatchPattern(attr.Endpoint, currentEndpoint));
        }

        private static bool MatchPattern(string template, string actual)
        {
            while (actual.StartsWith("/"))
            {
                actual = actual.Substring(1);
            }

            while (template.StartsWith("/"))
            {
                template = template.Substring(1);
            }

            if (actual == template)
                return true;

            var templateParts = template.Split('/');
            var actualParts = actual.Split('/');

            if (templateParts.Length != actualParts.Length)
                return false;

            for (int i = 0; i < templateParts.Length; i++)
            {
                var templatePart = templateParts[i];
                var actualPart = actualParts[i];

                if (templatePart.StartsWith("{") && templatePart.EndsWith("}"))
                    continue;

                if (templatePart != actualPart)
                    return false;
            }

            return true;
        }

        private string GetRouteTemplate()
        {
            var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint() as RouteEndpoint;
            return endpoint?.RoutePattern?.RawText ?? _httpContextAccessor.HttpContext?.Request.Path.Value ?? string.Empty;
        }

        private bool IsApiRequest()
        {
            if (_httpContextAccessor?.HttpContext == null)
            {
                return false;
            }

            var endpoint = _httpContextAccessor.HttpContext.GetEndpoint();
            if (endpoint == null)
            {
                return false;
            }

            var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerActionDescriptor != null)
            {
                return controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<ApiControllerAttribute>() != null;
            }

            return false;
        }
    }
}