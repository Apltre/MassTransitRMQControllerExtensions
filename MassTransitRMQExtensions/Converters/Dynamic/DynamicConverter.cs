using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MassTransitRMQExtensions.Converters.Dynamic
{
    public class DynamicConverter : JsonConverter<dynamic>
    {
        public override dynamic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!JsonDocument.TryParseValue(ref reader, out var document))
            {
                throw new JsonException("Failed to parse dynamic object.");
            }
            
            var obj = ReflectionDynamicObject.Create(document.RootElement);

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, dynamic value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}