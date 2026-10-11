using MassTransit.Transports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Identity.Contract.JsonConverters
{
    public class TypeNameHandlingConverter : JsonConverter
    {
        private readonly TypeNameHandling _typeNameHandling;

        public TypeNameHandlingConverter(TypeNameHandling typeNameHandling)
        {
            _typeNameHandling = typeNameHandling;
        }

        public override bool CanConvert(Type objectType)
            => IsMassTransitOrSystemType(objectType) is false;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        => new JsonSerializer { TypeNameHandling = _typeNameHandling }.Deserialize(reader, objectType);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => new JsonSerializer { TypeNameHandling = _typeNameHandling }.Serialize(writer, value);

        private bool IsMassTransitOrSystemType(Type type)
        => (type.Assembly.FullName?.Contains(nameof(MassTransit)) ?? false) ||
            type.Assembly.IsDynamic ||
            type.Assembly == typeof(object).Assembly;
    }
}

