using IqraCore.Attributes;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Concurrent;

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

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException("Deserialization is not implemented for this converter.");

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
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
                else if (property.PropertyType.IsPrimitive || propertyValue is string || propertyValue is DateTime)
                {
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
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

            writer.WriteEndObject();
        }

        private PropertyInfo[] GetSerializableProperties(Type type, string currentEndpoint)
        {
            return _propertyCache.GetOrAdd((type, currentEndpoint), key =>
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => ShouldSerializeProperty(p, currentEndpoint))
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
