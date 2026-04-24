using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Settings.Json;

/// <summary>
/// Serializes <see cref="ExtractionRule"/> as a JSON object with
/// <c>tag</c>, <c>whitelist</c>, and <c>blacklist</c> properties.
/// Required because <see cref="ExtractionRule"/> uses <see cref="IReadOnlySet{T}"/>
/// which STJ cannot auto-construct via parameterized constructor binding.
/// </summary>
public sealed class ExtractionRuleJsonConverter : JsonConverter<ExtractionRule>
{
    public override ExtractionRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for ExtractionRule.");

        string? tag = null;
        var whitelist = new List<string>();
        var blacklist = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "tag":
                    tag = reader.GetString();
                    break;
                case "whitelist":
                    ReadStringList(ref reader, whitelist);
                    break;
                case "blacklist":
                    ReadStringList(ref reader, blacklist);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (tag is null)
            throw new JsonException("ExtractionRule is missing required property 'tag'.");

        return new ExtractionRule(tag, whitelist, blacklist);
    }

    public override void Write(Utf8JsonWriter writer, ExtractionRule value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("tag", value.Tag);

        writer.WriteStartArray("whitelist");
        foreach (var item in value.Whitelist.OrderBy(x => x, StringComparer.Ordinal))
            writer.WriteStringValue(item);
        writer.WriteEndArray();

        writer.WriteStartArray("blacklist");
        foreach (var item in value.Blacklist.OrderBy(x => x, StringComparer.Ordinal))
            writer.WriteStringValue(item);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void ReadStringList(ref Utf8JsonReader reader, List<string> target)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            var item = reader.GetString() ?? throw new JsonException("Expected non-null string in array.");
            target.Add(item);
        }
    }
}
