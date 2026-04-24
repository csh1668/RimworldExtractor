namespace RimworldExtractor.Domain.Mods;

/// <summary>
/// A condition on which mods must / must not be loaded, expressed as AND-of-ORs.
/// Each inner list is an OR-group (any one satisfies); outer lists are ANDed.
/// Legacy DSL serialization (<c>" &amp;&amp; "</c> / <c>" || "</c> / <c>" ~~ "</c>) is handled in Infrastructure.
/// </summary>
public sealed record RequiredMods
{
    public IReadOnlyList<IReadOnlyList<ModReference>> Allowed { get; }
    public IReadOnlyList<IReadOnlyList<ModReference>> Disallowed { get; }

    public static RequiredMods Empty { get; } = new(
        Array.Empty<IReadOnlyList<ModReference>>(),
        Array.Empty<IReadOnlyList<ModReference>>());

    public RequiredMods(
        IEnumerable<IEnumerable<ModReference>> allowed,
        IEnumerable<IEnumerable<ModReference>> disallowed)
    {
        Allowed = allowed.Select(g => (IReadOnlyList<ModReference>)g.ToArray()).ToArray();
        Disallowed = disallowed.Select(g => (IReadOnlyList<ModReference>)g.ToArray()).ToArray();
    }

    public bool IsEmpty => Allowed.Count == 0 && Disallowed.Count == 0;

    public RequiredMods Combine(RequiredMods? other)
    {
        if (other is null) return this;
        return new RequiredMods(
            allowed: Allowed.Concat(other.Allowed),
            disallowed: Disallowed.Concat(other.Disallowed));
    }

    public bool Equals(RequiredMods? other)
    {
        if (other is null) return false;
        return SequenceEquals(Allowed, other.Allowed) && SequenceEquals(Disallowed, other.Disallowed);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var group in Allowed)
            foreach (var r in group) hash.Add(r);
        foreach (var group in Disallowed)
            foreach (var r in group) hash.Add(r);
        return hash.ToHashCode();
    }

    private static bool SequenceEquals(
        IReadOnlyList<IReadOnlyList<ModReference>> a,
        IReadOnlyList<IReadOnlyList<ModReference>> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].SequenceEqual(b[i])) return false;
        return true;
    }
}
