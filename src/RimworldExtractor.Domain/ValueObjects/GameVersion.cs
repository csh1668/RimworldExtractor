using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RimworldExtractor.Domain.Settings.Json;

namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// RimWorld major.minor version (e.g. <c>1.6</c>). Only the 1.x series is supported
/// per legacy <c>Prefabs.PatternVersion</c> regex.
/// </summary>
[JsonConverter(typeof(GameVersionJsonConverter))]
public readonly record struct GameVersion : IComparable<GameVersion>
{
    private static readonly Regex BareForm = new(@"^1\.(\d+)$", RegexOptions.Compiled);
    private static readonly Regex VPrefixedForm = new(@"^v1\.(\d+)$", RegexOptions.Compiled);

    public int Major { get; }
    public int Minor { get; }

    private GameVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public static GameVersion Parse(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new FormatException("GameVersion cannot be empty.");
        var match = BareForm.Match(value);
        if (!match.Success)
            throw new FormatException($"GameVersion '{value}' does not match pattern '1.MINOR'.");
        return new GameVersion(1, int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    public static bool TryParseAny(string value, out GameVersion result)
    {
        result = default;
        if (string.IsNullOrEmpty(value)) return false;

        var m = BareForm.Match(value);
        if (!m.Success) m = VPrefixedForm.Match(value);
        if (!m.Success) return false;

        result = new GameVersion(1, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        return true;
    }

    public int CompareTo(GameVersion other)
    {
        var majorCompare = Major.CompareTo(other.Major);
        return majorCompare != 0 ? majorCompare : Minor.CompareTo(other.Minor);
    }

    public static bool operator <(GameVersion left, GameVersion right) => left.CompareTo(right) < 0;
    public static bool operator <=(GameVersion left, GameVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >(GameVersion left, GameVersion right) => left.CompareTo(right) > 0;
    public static bool operator >=(GameVersion left, GameVersion right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Major}.{Minor}";
}
