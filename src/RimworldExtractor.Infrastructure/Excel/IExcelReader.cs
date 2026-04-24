using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Infrastructure.Excel;

/// <summary>
/// Reads an XLSX file produced by <see cref="IExcelWriter"/> (or the legacy ToExcel method)
/// and returns the contained <see cref="TranslationEntry"/> records.
/// </summary>
public interface IExcelReader
{
    /// <summary>
    /// Parses the XLSX at <paramref name="path"/> and returns all data rows as
    /// <see cref="TranslationEntry"/> instances.
    /// </summary>
    /// <param name="path">Path to the XLSX file.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <exception cref="InvalidDataException">
    /// Thrown when a required header column (Class, Node, Original, or Translated) is absent.
    /// </exception>
    Task<IReadOnlyList<TranslationEntry>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default);
}
