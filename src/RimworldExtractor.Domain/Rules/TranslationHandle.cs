namespace RimworldExtractor.Domain.Rules;

/// <summary>
/// A translation-handle rule from legacy <c>Prefabs.TranslationHandles</c>. A handle tells the
/// extractor to dive into a sub-node referenced by a class attribute. A leading <c>*</c>
/// in the raw form means "any class name" (wildcard).
/// </summary>
public sealed record TranslationHandle
{
    public string Tag { get; }
    public bool IsWildcardClass { get; }

    private TranslationHandle(string tag, bool isWildcardClass)
    {
        Tag = tag;
        IsWildcardClass = isWildcardClass;
    }

    public static TranslationHandle Parse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("TranslationHandle must be non-empty.", nameof(raw));
        var wildcard = raw.StartsWith('*');
        var tag = wildcard ? raw[1..] : raw;
        if (tag.Length == 0)
            throw new ArgumentException("TranslationHandle tag must be non-empty.", nameof(raw));
        return new TranslationHandle(tag, wildcard);
    }

    public override string ToString() => IsWildcardClass ? "*" + Tag : Tag;
}
