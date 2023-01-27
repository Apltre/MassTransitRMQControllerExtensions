using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MassTransitRMQExtensions.Converters.Dynamic
{
    public class ReflectionDynamicConverter: JsonConverter<ReflectionDynamicObject>
    {
        public override ReflectionDynamicObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(!JsonDocument.TryParseValue(ref reader, out var document))
            {
                throw new JsonException("Failed to parse dynamic object.");
            } 

            return new ReflectionDynamicObject(document.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, ReflectionDynamicObject value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.RealObject, value.RealObject.GetType(), options);
        }
    }
}