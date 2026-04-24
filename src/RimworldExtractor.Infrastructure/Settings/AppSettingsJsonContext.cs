using System.Text.Json.Serialization;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Settings;

/// <summary>
/// System.Text.Json source-generated context for the AppSettings tree.
/// Emits AOT-safe serialization that does not require runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(PathSettings))]
[JsonSerializable(typeof(LanguageSettings))]
[JsonSerializable(typeof(ExtractionSettings))]
[JsonSerializable(typeof(OutputSettings))]
[JsonSerializable(typeof(ExtractionRule))]
[JsonSerializable(typeof(NodeReplacementRule))]
[JsonSerializable(typeof(TranslationHandle))]
[JsonSerializable(typeof(GameVersion))]
[JsonSerializable(typeof(LanguageCode))]
[JsonSerializable(typeof(DuplicatesPolicy))]
[JsonSerializable(typeof(ExtractionFormat))]
public sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
