namespace RimworldExtractor.Domain.Mods;

public enum ModReferenceKind
{
    PackageId = 0,
    ModName = 1,
}

public sealed record ModReference
{
    public string Value { get; }
    public ModReferenceKind Kind { get; }

    public ModReference(string value, ModReferenceKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ModReference value must be non-empty.", nameof(value));
        Value = value;
        Kind = kind;
    }

    public static ModReference ByPackageId(string packageId) => new(packageId, ModReferenceKind.PackageId);
    public static ModReference ByModName(string modName) => new(modName, ModReferenceKind.ModName);
}
