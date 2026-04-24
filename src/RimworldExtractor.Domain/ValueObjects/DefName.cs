namespace RimworldExtractor.Domain.ValueObjects;

/// <summary>
/// Strongly-typed RimWorld def identifier (the content of <c>&lt;defName&gt;</c>). Non-empty, trimmed.
/// </summary>
public readonly record struct DefName
{
    public string Value { get; }

    private DefName(string value) => Value = value;

    public static DefName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("DefName must be non-empty and not whitespace.", nameof(value));
        return new DefName(value);
    }

    public override string ToString() => Value;
}
