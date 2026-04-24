using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;

namespace RimworldExtractor.Infrastructure.Excel;

/// <summary>
/// Writes a collection of <see cref="TranslationEntry"/> records to an XLSX file.
/// </summary>
public interface IExcelWriter
{
    /// <summary>
    /// Serializes <paramref name="translations"/> to an XLSX file at <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="translations">Entries to write (may be empty).</param>
    /// <param name="outputPath">Destination file path (created or overwritten atomically).</param>
    /// <param name="originalLanguage">Language of the source strings (written to column 5 header).</param>
    /// <param name="translationLanguage">Language of the translated strings (written to column 6 header).</param>
    /// <param name="markNoTranslation">
    /// When <see langword="true"/>, cells in the translated column that have no value are highlighted
    /// with a sky-blue background so they are easy to spot in Excel.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string outputPath,
        LanguageCode originalLanguage,
        LanguageCode translationLanguage,
        bool markNoTranslation = false,
        CancellationToken cancellationToken = default);
}
