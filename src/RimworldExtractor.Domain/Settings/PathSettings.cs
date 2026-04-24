namespace RimworldExtractor.Domain.Settings;

public sealed record PathSettings(
    string Rimworld,
    string Workshop,
    string BaseRefList)
{
    public static PathSettings Default { get; } = new("", "", "");
}
