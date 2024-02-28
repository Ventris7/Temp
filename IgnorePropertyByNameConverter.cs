using System.Text.Json;
using System.Text.Json.Serialization;

public class IgnorePropertyByNameConverter<T> : JsonConverter<T>
{
    private readonly HashSet<string> _propertiesToIgnore;

    public IgnorePropertyByNameConverter(IEnumerable<string> propertiesToIgnore)
    {
        _propertiesToIgnore = new HashSet<string>(propertiesToIgnore);
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (_propertiesToIgnore.Contains(property.Name)) continue;

            var propertyValue = property.GetValue(value);
            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, options);
        }

        writer.WriteEndObject();
    }
}
