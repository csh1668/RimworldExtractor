using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Legacy;

/// <summary>
/// Reads a legacy <c>Prefabs.dat</c> (line-delimited text, schema version 9) into the
/// modern <see cref="AppSettings"/> shape. Read-only; does not write back to the legacy format.
/// </summary>
public static class LegacyPrefabsReader
{
    private const string SupportedVersion = "9";

    public static AppSettings Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Legacy Prefabs.dat not found: {path}", path);

        var lines = File.ReadAllLines(path);
        if (lines.Length < 18)
            throw new InvalidDataException($"Prefabs.dat is truncated (expected 18 lines, got {lines.Length}).");

        // Line 0: header marker — ignored.
        var version = lines[1];
        if (version != SupportedVersion)
            throw new InvalidDataException(
                $"Unsupported Prefabs.dat version '{version}'. Supported: '{SupportedVersion}'.");

        var enableTkey = bool.Parse(lines[2]);
        var pathRw = lines[3];
        var pathWs = lines[4];
        var pathBase = lines[5];
        var currentVersion = GameVersion.Parse(lines[6]);
        // Lines 7-8 (patternVersion, patternVersionWithV) are intentionally dropped.
        var origLang = LanguageCode.Create(lines[9]);
        var transLang = LanguageCode.Create(lines[10]);
        var commentOriginal = bool.Parse(lines[11]);

        var rules = ParseList(lines[12], LegacyExtractionRuleParser.Parse).ToList();
        var fullListTags = ParseList(lines[13], s => s).ToList();
        var nodeReplacements = ParseList(lines[14], ParseNodeReplacement).ToList();
        var translationHandles = ParseList(lines[15], TranslationHandle.Parse).ToList();

        var policy = Enum.Parse<DuplicatesPolicy>(lines[16]);
        // Legacy enum name was "ExtractionMethod" with identical member names.
        var format = Enum.Parse<ExtractionFormat>(lines[17]);

        return new AppSettings(
            SchemaVersion: AppSettings.CurrentSchemaVersion,
            Paths: new PathSettings(pathRw, pathWs, pathBase),
            Languages: new LanguageSettings(origLang, transLang),
            Extraction: new ExtractionSettings(
                CurrentVersion: currentVersion,
                CommentOriginal: commentOriginal,
                EnableTkey: enableTkey,
                Rules: rules,
                FullListTags: fullListTags,
                NodeReplacements: nodeReplacements,
                TranslationHandles: translationHandles),
            Output: new OutputSettings(policy, format));
    }

    private static IEnumerable<T> ParseList<T>(string line, Func<string, T> parse)
    {
        if (string.IsNullOrEmpty(line)) return [];
        return line.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(parse);
    }

    private static NodeReplacementRule ParseNodeReplacement(string raw)
    {
        var pipe = raw.IndexOf('|');
        if (pipe <= 0 || pipe == raw.Length - 1)
            throw new FormatException($"NodeReplacement entry missing '|' separator: {raw}");
        var from = raw[..pipe].Trim();
        var to = raw[(pipe + 1)..].Trim();
        return new NodeReplacementRule(from, to);
    }
}
