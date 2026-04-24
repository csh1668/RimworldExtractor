using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Domain.Settings.Json;

public sealed class GameVersionJsonConverter : JsonConverter<GameVersion>
{
    public override GameVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected GameVersion string.");
        return GameVersion.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, GameVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
