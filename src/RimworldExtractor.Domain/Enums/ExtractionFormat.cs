namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// Output format choice for a translation extraction.
/// Ordinals preserved from legacy Prefabs.dat (was named <c>ExtractionMethod</c>).
/// </summary>
public enum ExtractionFormat
{
    Excel = 0,
    Languages = 1,
    LanguagesWithComments = 2,
}
