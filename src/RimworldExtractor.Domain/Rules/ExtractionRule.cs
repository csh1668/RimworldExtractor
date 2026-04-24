namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// A rule deciding whether a given XML tag under a given DefName should be treated as translatable.
/// Whitelist restricts to specific DefNames; Blacklist excludes specific DefNames (Blacklist beats Whitelist).
/// </summary>
public sealed record ExtractionRule
{
    public string Tag { get; }
    public IReadOnlySet<string> Whitelist { get; }
    public IReadOnlySet<string> Blacklist { get; }

    public ExtractionRule(
        string tag,
        IEnumerable<string>? whitelist = null,
        IEnumerable<string>? blacklist = null)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag must be non-empty.", nameof(tag));
        Tag = tag;
        Whitelist = whitelist?.ToHashSet() ?? (IReadOnlySet<string>)new HashSet<string>();
        Blacklist = blacklist?.ToHashSet() ?? (IReadOnlySet<string>)new HashSet<string>();
    }

    public bool CanExtract(string defName)
    {
        if (Whitelist.Count > 0 && !Whitelist.Contains(defName)) return false;
        if (Blacklist.Contains(defName)) return false;
        return true;
    }

    public bool Equals(ExtractionRule? other)
    {
        if (other is null) return false;
        return Tag == other.Tag
            && Whitelist.SetEquals(other.Whitelist)
            && Blacklist.SetEquals(other.Blacklist);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tag);
        foreach (var w in Whitelist.OrderBy(x => x, StringComparer.Ordinal)) hash.Add(w);
        foreach (var b in Blacklist.OrderBy(x => x, StringComparer.Ordinal)) hash.Add(b);
        return hash.ToHashCode();
    }
}
