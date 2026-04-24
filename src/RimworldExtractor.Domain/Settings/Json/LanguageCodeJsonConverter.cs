using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings.Json;

public sealed class LanguageCodeJsonConverter : JsonConverter<LanguageCode>
{
    public override LanguageCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected LanguageCode string.");
        return LanguageCode.Create(raw);
    }

    public override void Write(Utf8JsonWriter writer, LanguageCode value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Display);
}
