namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// RimWorld packageId (e.g. <c>Ludeon.RimWorld</c>). Comparison is case-insensitive; display form preserves original casing.
/// </summary>
public readonly record struct PackageId
{
    public string Value { get; }
    public string Normalized { get; }

    private PackageId(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public static PackageId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PackageId must be non-empty.", nameof(value));
        if (!value.Contains('.'))
            throw new ArgumentException("PackageId must contain at least one '.' separator.", nameof(value));
        return new PackageId(value, value.ToLowerInvariant());
    }

    public bool Equals(PackageId other) => Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
