using System.Text.Json;
using System.Text.Json.Serialization;
using RimworldExtractor.Domain.Rules;

namespace RimworldExtractor.Domain.Settings.Json;

/// <summary>
/// Serializes <see cref="TranslationHandle"/> as a raw string (e.g. <c>*verbClass</c> or <c>label</c>)
/// using <see cref="TranslationHandle.ToString"/> and <see cref="TranslationHandle.Parse"/>.
/// Required because <see cref="TranslationHandle"/> has only a private constructor.
/// </summary>
public sealed class TranslationHandleJsonConverter : JsonConverter<TranslationHandle>
{
    public override TranslationHandle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? throw new JsonException("Expected TranslationHandle string.");
        return TranslationHandle.Parse(raw);
    }

    public override void Write(Utf8JsonWriter writer, TranslationHandle value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
