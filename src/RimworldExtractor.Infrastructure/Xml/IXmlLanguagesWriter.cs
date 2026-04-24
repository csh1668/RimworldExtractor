using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Xml;

public interface IXmlLanguagesWriter
{
    /// <summary>Writes the given translations to a RimWorld Languages-format tree rooted at <paramref name="rootDir"/>.</summary>
    /// <param name="translations">The entries to write.</param>
    /// <param name="rootDir">Output root (typically mod root or a "Translation" workspace).</param>
    /// <param name="translationLanguage">Target language (folder name + display).</param>
    /// <param name="fileName">Base filename for DefInjected/Keyed/Patches output files.</param>
    /// <param name="skipNoTranslation">If true, skip entries whose Translated is null/empty (except Strings).</param>
    /// <param name="commentOriginal">If true, emit Original text as XML comments inline.</param>
    /// <param name="fullListTags">Tag names subject to DoFullListTranslation collapsing (e.g. rulesStrings).</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string rootDir,
        LanguageCode translationLanguage,
        string fileName,
        bool skipNoTranslation,
        bool commentOriginal,
        IReadOnlyList<string> fullListTags,
        CancellationToken cancellationToken = default);
}
