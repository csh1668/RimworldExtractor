namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// Maps a source Def-class pattern (e.g. <c>CombatExtended.AmmoDef+*</c>) to a replacement
/// pattern (e.g. <c>ThingDef+*</c>) so extraction treats mod classes as base-class entries.
/// </summary>
public sealed record NodeReplacementRule
{
    public string From { get; }
    public string To { get; }

    public NodeReplacementRule(string from, string to)
    {
        if (string.IsNullOrEmpty(from))
            throw new ArgumentException("From pattern must be non-empty.", nameof(from));
        if (string.IsNullOrEmpty(to))
            throw new ArgumentException("To pattern must be non-empty.", nameof(to));
        From = from;
        To = to;
    }
}
