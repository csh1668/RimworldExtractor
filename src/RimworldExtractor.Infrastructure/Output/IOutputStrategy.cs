using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Output;

/// <summary>
/// Writes extraction results in a specific output format. Implementations are keyed by
/// <see cref="ExtractionFormat"/> via DI and selected at runtime by <c>AppSettings.Output.Format</c>.
/// </summary>
public interface IOutputStrategy
{
    ExtractionFormat Format { get; }

    Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string outputRoot,
        string baseFileName,
        LanguageCode originalLanguage,
        LanguageCode translationLanguage,
        CancellationToken cancellationToken = default);
}
