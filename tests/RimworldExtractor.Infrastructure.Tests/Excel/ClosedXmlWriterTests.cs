using ClosedXML.Excel;
using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.Excel;

/// <summary>
/// Unit tests for <see cref="ClosedXmlWriter"/>.
/// Uses a real temporary file because ClosedXML requires seekable disk-backed streams
/// for reliable SaveAs behavior.
/// </summary>
public class ClosedXmlWriterTests
{
    private static readonly LanguageCode English = LanguageCode.Create("English");
    private static readonly LanguageCode Korean = LanguageCode.Create("Korean (한국어)");

    private static ClosedXmlWriter CreateWriter(string tempDir)
    {
        var fs = new PhysicalFileSystem();
        var safeWriter = new SafeFileWriter(fs);
        return new ClosedXmlWriter(safeWriter);
    }

    private static string TempXlsxPath()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"WriterTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "out.xlsx");
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task WriteAsync_EmptyList_WritesOnlyHeaders()
    {
        string path = TempXlsxPath();
        try
        {
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync([], path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);
            var usedRows = sheet.RowsUsed().ToList();

            usedRows.Should().HaveCount(1, "only the header row should be present");
            sheet.Cell(1, 1).Value.GetText().Should().Be("Class+Node [(Identifier (Key)]");
            sheet.Cell(1, 2).Value.GetText().Should().Be("Class [Not chosen]");
            sheet.Cell(1, 3).Value.GetText().Should().Be("Node [Not chosen]");
            sheet.Cell(1, 4).Value.GetText().Should().Be("Required Mods [Not chosen]");
            sheet.Cell(1, 5).Value.GetText().Should().Be("English [Source string]");
            sheet.Cell(1, 6).Value.GetText().Should().Be("Korean (한국어) [Translation]");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_SingleEntry_WritesCellsCorrectly()
    {
        string path = TempXlsxPath();
        try
        {
            var entry = new TranslationEntry("ThingDef", "SteelDef.label", "Steel", "강철", null, null);
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync([entry], path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);

            sheet.Cell(2, 1).Value.GetText().Should().Be("ThingDef+SteelDef.label");
            sheet.Cell(2, 2).Value.GetText().Should().Be("ThingDef");
            sheet.Cell(2, 3).Value.GetText().Should().Be("SteelDef.label");
            sheet.Cell(2, 4).Value.IsBlank.Should().BeTrue("RequiredMods is null");
            sheet.Cell(2, 5).Value.GetText().Should().Be("Steel");
            sheet.Cell(2, 6).Value.GetText().Should().Be("강철");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_EntryWithNullTranslation_LeavesTranslatedCellBlank()
    {
        string path = TempXlsxPath();
        try
        {
            var entry = new TranslationEntry("ThingDef", "SteelDef.label", "Steel", null, null, null);
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync([entry], path, English, Korean,
                markNoTranslation: false,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);

            sheet.Cell(2, 6).Value.IsBlank.Should().BeTrue("untranslated with markNoTranslation=false should be blank");
            sheet.Cell(2, 6).Style.Fill.BackgroundColor.Should().NotBe(XLColor.SkyBlue);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_MarkNoTranslation_ColorsNullTranslationCellSkyBlue()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef", "A.label", "Alpha", null, null, null),
                new TranslationEntry("ThingDef", "B.label", "Beta", "베타", null, null),
            };
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync(entries, path, English, Korean,
                markNoTranslation: true,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);

            // Row 2 (null translation) → sky blue
            sheet.Cell(2, 6).Style.Fill.BackgroundColor.Should().Be(XLColor.SkyBlue);
            // Row 3 (has translation) → no sky blue
            sheet.Cell(3, 6).Style.Fill.BackgroundColor.Should().NotBe(XLColor.SkyBlue);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_EntryWithRequiredMods_WritesDisplayString()
    {
        string path = TempXlsxPath();
        try
        {
            var mods = new RequiredMods(
                allowed: [[ModReference.ByModName("Mod A"), ModReference.ByModName("Mod B")]],
                disallowed: []);
            var entry = new TranslationEntry("ThingDef", "SteelDef.label", "Steel", "강철", mods, null);
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync([entry], path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);

            string cell4 = sheet.Cell(2, 4).Value.GetText();
            cell4.Should().Contain("Mod A").And.Contain("Mod B");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_MultipleEntries_RowsAreOrderedCorrectly()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef", "A.label", "Alpha", "알파", null, null),
                new TranslationEntry("ThingDef", "B.label", "Beta", "베타", null, null),
                new TranslationEntry("Keyed", "C", "Charlie", null, null, null),
            };
            var writer = CreateWriter(Path.GetDirectoryName(path)!);
            await writer.WriteAsync(entries, path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            using var wb = new XLWorkbook(path);
            IXLWorksheet sheet = wb.Worksheets.Worksheet(1);

            sheet.Cell(2, 2).Value.GetText().Should().Be("ThingDef");
            sheet.Cell(3, 2).Value.GetText().Should().Be("ThingDef");
            sheet.Cell(4, 2).Value.GetText().Should().Be("Keyed");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
