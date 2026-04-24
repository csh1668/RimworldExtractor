using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Xml;

public interface IXmlLanguagesReader
{
    /// <summary>Reads back a Languages-format directory tree and returns all <see cref="TranslationEntry"/> records found.</summary>
    /// <param name="rootDir">The directory that contains the <c>Languages/</c> subfolder.</param>
    /// <param name="translationLanguage">The target language whose folder to read.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<IReadOnlyList<TranslationEntry>> ReadAsync(
        string rootDir,
        LanguageCode translationLanguage,
        CancellationToken cancellationToken = default);
}
