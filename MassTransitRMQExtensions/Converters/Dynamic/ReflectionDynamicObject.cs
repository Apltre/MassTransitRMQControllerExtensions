using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MassTransitRMQExtensions.Converters.Dynamic
{
    [JsonConverter(typeof(ReflectionDynamicConverter))]
    public class ReflectionDynamicObject : DynamicObject
    {
        internal JsonElement RealObject { get; set; }

        public static dynamic Create(JsonElement realObject)
        {
            return parse(realObject);
        }
            
        internal ReflectionDynamicObject(JsonElement realObject)
        {
            this.RealObject = realObject;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var srcData = RealObject.GetProperty(binder.Name);

            result = parse(srcData);

            return true;
        }

        private static dynamic parse(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Object => new ReflectionDynamicObject(element),
                JsonValueKind.Array => element.EnumerateArray().Select(o => parse(o)).ToArray(),
                _ => throw new UnsupportedPropertyTypeException($"Unsupported property type: {element.ValueKind}."),
            };
        }
    }
}