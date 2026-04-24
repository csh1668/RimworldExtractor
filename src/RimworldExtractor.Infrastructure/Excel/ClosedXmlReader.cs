using ClosedXML.Excel;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Infrastructure.Excel;

/// <summary>
/// <see cref="IExcelReader"/> implementation backed by ClosedXML.
/// Port of the legacy <c>IO.FromExcel</c> method.
/// </summary>
/// <remarks>
/// The file is first preprocessed by <see cref="LibreOfficePostProcessor"/> to strip
/// LibreOffice comment artefacts that cause ClosedXML to throw.
/// </remarks>
public sealed class ClosedXmlReader : IExcelReader
{
    private const string HeaderClass = "Class [Not chosen]";
    private const string HeaderNode = "Node [Not chosen]";
    private const string HeaderRequiredMods = "Required Mods [Not chosen]";

    // Fallback header names preserved for backward-compatibility with files produced by the legacy tool.
    private const string FallbackHeaderOriginal = "EN [Source string]";
    private const string FallbackHeaderTranslated = "KO [Translation]";

    /// <inheritdoc/>
    public Task<IReadOnlyList<TranslationEntry>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        // LibreOfficePostProcessor is synchronous (ZipArchive IO).  Run on the thread pool
        // so we don't block the caller's async context.
        return Task.Run(() => Read(path), cancellationToken);
    }

    private static IReadOnlyList<TranslationEntry> Read(string path)
    {
        // Pre-process: strip LibreOffice comment artefacts.
        string workingPath;
        LibreOfficePostProcessor? proc = null;
        try
        {
            proc = new LibreOfficePostProcessor(path);
            workingPath = proc.Process();
        }
        catch
        {
            proc?.Dispose();
            throw;
        }

        try
        {
            using var workbook = new XLWorkbook(workingPath);
            IXLWorksheet sheet = workbook.Worksheets.Worksheet(1);
            var rows = sheet.RowsUsed().ToList();

            if (rows.Count == 0)
                return Array.Empty<TranslationEntry>();

            // Detect column indices from the header row.
            IXLCells headers = rows[0].Cells();

            int colClass = FindColumn(headers, HeaderClass)
                ?? throw new InvalidDataException($"Required header column '{HeaderClass}' not found.");
            int colNode = FindColumn(headers, HeaderNode)
                ?? throw new InvalidDataException($"Required header column '{HeaderNode}' not found.");

            // Required Mods is optional — returns -1 when absent.
            int colRequiredMods = FindColumn(headers, HeaderRequiredMods) ?? -1;

            int colOriginal = FindColumnWithFallback(headers, BuildOriginalHeaders(headers))
                ?? throw new InvalidDataException($"Required header column for original text not found (expected a column ending with '[Source string]').");

            int colTranslated = FindColumnWithFallback(headers, BuildTranslatedHeaders(headers))
                ?? throw new InvalidDataException($"Required header column for translated text not found (expected a column ending with '[Translation]').");

            var results = new List<TranslationEntry>(rows.Count - 1);

            for (int i = 1; i < rows.Count; i++)
            {
                IXLRow row = rows[i];

                string className = CellText(row, colClass);
                string node = CellText(row, colNode);

                // Skip rows where class or node is blank.
                if (className.Length == 0 || node.Length == 0)
                    continue;

                // RequiredMods parsing is complex (legacy DSL); defer to a later phase.
                // For now we always emit null.
                string original = row.Cell(colOriginal).Value.IsBlank
                    ? string.Empty
                    : CellText(row, colOriginal);

                IXLCell translatedCell = row.Cell(colTranslated);
                string? translated = translatedCell.Value.IsText
                    ? (translatedCell.Value.GetText() == string.Empty ? null : translatedCell.Value.GetText())
                    : null;

                results.Add(new TranslationEntry(className, node, original, translated,
                    RequiredMods: null, SourceFile: null));
            }

            return results;
        }
        finally
        {
            proc.Dispose();
        }
    }

    // -----------------------------------------------------------------------
    // Header-detection helpers
    // -----------------------------------------------------------------------

    private static int? FindColumn(IXLCells headers, string name)
    {
        IXLCell? cell = headers.FirstOrDefault(c => CellText(c) == name);
        return cell is null ? null : cell.WorksheetColumn().ColumnNumber();
    }

    /// <summary>
    /// Returns candidate header names to try when locating the Original column.
    /// The primary candidates are any headers ending with "[Source string]"; the
    /// legacy English-only fallback is tried last.
    /// </summary>
    private static IEnumerable<string> BuildOriginalHeaders(IXLCells headers)
    {
        foreach (IXLCell cell in headers)
        {
            string text = CellText(cell);
            if (text.EndsWith("[Source string]", StringComparison.Ordinal))
                yield return text;
        }
        yield return FallbackHeaderOriginal;
    }

    /// <summary>
    /// Returns candidate header names to try when locating the Translated column.
    /// The primary candidates are any headers ending with "[Translation]"; the
    /// legacy Korean-only fallback is tried last.
    /// </summary>
    private static IEnumerable<string> BuildTranslatedHeaders(IXLCells headers)
    {
        foreach (IXLCell cell in headers)
        {
            string text = CellText(cell);
            if (text.EndsWith("[Translation]", StringComparison.Ordinal))
                yield return text;
        }
        yield return FallbackHeaderTranslated;
    }

    private static int? FindColumnWithFallback(IXLCells headers, IEnumerable<string> candidates)
    {
        foreach (string candidate in candidates)
        {
            int? col = FindColumn(headers, candidate);
            if (col.HasValue) return col;
        }
        return null;
    }

    private static string CellText(IXLRow row, int col)
    {
        IXLCell cell = row.Cell(col);
        return cell.Value.IsText ? cell.Value.GetText() : string.Empty;
    }

    private static string CellText(IXLCell cell)
    {
        return cell.Value.IsText ? cell.Value.GetText() : string.Empty;
    }
}
