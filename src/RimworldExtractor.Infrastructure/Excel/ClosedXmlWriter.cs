using ClosedXML.Excel;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Legacy;

namespace RimworldExtractor.Infrastructure.Excel;

/// <summary>
/// <see cref="IExcelWriter"/> implementation backed by ClosedXML.
/// Port of the legacy <c>IO.ToExcel</c> method.
/// </summary>
/// <remarks>
/// The workbook is built in memory then flushed atomically via <see cref="SafeFileWriter"/>
/// so a crash mid-write never leaves a partially-written XLSX on disk.
/// </remarks>
public sealed class ClosedXmlWriter : IExcelWriter
{
    private static readonly string HeaderClassNode = "Class+Node [(Identifier (Key)]";
    private static readonly string HeaderClass = "Class [Not chosen]";
    private static readonly string HeaderNode = "Node [Not chosen]";
    private static readonly string HeaderRequiredMods = "Required Mods [Not chosen]";

    private readonly SafeFileWriter _fileWriter;

    public ClosedXmlWriter(SafeFileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(
        IReadOnlyList<TranslationEntry> translations,
        string outputPath,
        LanguageCode originalLanguage,
        LanguageCode translationLanguage,
        bool markNoTranslation = false,
        CancellationToken cancellationToken = default)
    {
        string headerOriginal = $"{originalLanguage.Display} [Source string]";
        string headerTranslated = $"{translationLanguage.Display} [Translation]";

        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet();

        // Row 1: headers
        sheet.Cell(1, 1).Value = HeaderClassNode;
        sheet.Cell(1, 2).Value = HeaderClass;
        sheet.Cell(1, 3).Value = HeaderNode;
        sheet.Cell(1, 4).Value = HeaderRequiredMods;
        sheet.Cell(1, 5).Value = headerOriginal;
        sheet.Cell(1, 6).Value = headerTranslated;

        // Data rows
        for (int i = 0; i < translations.Count; i++)
        {
            TranslationEntry entry = translations[i];
            int row = 2 + i;

            sheet.Cell(row, 1).Value = $"{entry.ClassName}+{entry.Node}";
            sheet.Cell(row, 2).Value = entry.ClassName;
            sheet.Cell(row, 3).Value = entry.Node;
            string requiredModsDisplay = RequiredModsDisplay.ToDisplayString(entry.RequiredMods);
            if (requiredModsDisplay.Length > 0)
                sheet.Cell(row, 4).Value = requiredModsDisplay;
            sheet.Cell(row, 5).Value = entry.Original;

            if (entry.Translated is not null)
            {
                sheet.Cell(row, 6).Value = entry.Translated;
            }
            else if (markNoTranslation)
            {
                sheet.Cell(row, 6).Style.Fill.SetBackgroundColor(XLColor.SkyBlue);
            }
        }

        sheet.Style.Font.FontName = "맑은 고딕";

        // Write atomically via SafeFileWriter.
        await _fileWriter.WriteAsync(outputPath, stream =>
        {
            workbook.SaveAs(stream);
            return Task.CompletedTask;
        }, cancellationToken);
    }
}
