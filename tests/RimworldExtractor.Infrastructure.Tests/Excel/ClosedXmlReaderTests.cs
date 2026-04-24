using ClosedXML.Excel;
using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.Excel;

/// <summary>
/// Unit tests for <see cref="ClosedXmlReader"/>.
/// </summary>
public class ClosedXmlReaderTests
{
    private static readonly LanguageCode English = LanguageCode.Create("English");
    private static readonly LanguageCode Korean = LanguageCode.Create("Korean (한국어)");

    private static ClosedXmlWriter CreateWriter()
    {
        var fs = new PhysicalFileSystem();
        return new ClosedXmlWriter(new SafeFileWriter(fs));
    }

    private static ClosedXmlReader CreateReader() => new();

    private static string TempXlsxPath()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"ReaderTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "test.xlsx");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an XLSX on disk with explicit columns so tests can control the exact layout
    /// without going through ClosedXmlWriter.
    /// </summary>
    private static string WriteRawXlsx(string path,
        Action<IXLWorksheet> populate)
    {
        using var wb = new XLWorkbook();
        IXLWorksheet sheet = wb.AddWorksheet();
        populate(sheet);
        wb.SaveAs(path);
        return path;
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ReadAsync_WriteViaWriter_ReadsBackSameEntries()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef", "SteelDef.label", "Steel", "강철", null, null),
                new TranslationEntry("Keyed", "MyKey", "Hello", "안녕", null, null),
            };

            await CreateWriter().WriteAsync(entries, path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(2);
            read[0].ClassName.Should().Be("ThingDef");
            read[0].Node.Should().Be("SteelDef.label");
            read[0].Original.Should().Be("Steel");
            read[0].Translated.Should().Be("강철");
            read[1].ClassName.Should().Be("Keyed");
            read[1].Translated.Should().Be("안녕");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_NullTranslatedCell_ReturnsNullTranslated()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef", "X.label", "text", null, null, null),
            };
            await CreateWriter().WriteAsync(entries, path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(1);
            read[0].Translated.Should().BeNull();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingClassColumn_ThrowsInvalidDataException()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                // Intentionally omit the Class header.
                sheet.Cell(1, 1).Value = "Node [Not chosen]";
                sheet.Cell(1, 2).Value = "English [Source string]";
                sheet.Cell(1, 3).Value = "Korean (한국어) [Translation]";
            });

            var reader = CreateReader();
            await reader.Invoking(r => r.ReadAsync(path, TestContext.Current.CancellationToken))
                .Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Class*");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingNodeColumn_ThrowsInvalidDataException()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                sheet.Cell(1, 1).Value = "Class [Not chosen]";
                // Node column absent.
                sheet.Cell(1, 2).Value = "English [Source string]";
                sheet.Cell(1, 3).Value = "Korean (한국어) [Translation]";
            });

            var reader = CreateReader();
            await reader.Invoking(r => r.ReadAsync(path, TestContext.Current.CancellationToken))
                .Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Node*");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingOriginalColumn_ThrowsInvalidDataException()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                sheet.Cell(1, 1).Value = "Class [Not chosen]";
                sheet.Cell(1, 2).Value = "Node [Not chosen]";
                // Original column absent.
                sheet.Cell(1, 3).Value = "Korean (한국어) [Translation]";
            });

            var reader = CreateReader();
            await reader.Invoking(r => r.ReadAsync(path, TestContext.Current.CancellationToken))
                .Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*original*");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_RowWithEmptyClass_IsSkipped()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                sheet.Cell(1, 1).Value = "Class [Not chosen]";
                sheet.Cell(1, 2).Value = "Node [Not chosen]";
                sheet.Cell(1, 3).Value = "English [Source string]";
                sheet.Cell(1, 4).Value = "Korean (한국어) [Translation]";

                // Row 2: valid
                sheet.Cell(2, 1).Value = "ThingDef";
                sheet.Cell(2, 2).Value = "X.label";
                sheet.Cell(2, 3).Value = "text";
                sheet.Cell(2, 4).Value = "텍스트";

                // Row 3: empty class → should be skipped
                sheet.Cell(3, 1).Value = "";
                sheet.Cell(3, 2).Value = "Y.label";
                sheet.Cell(3, 3).Value = "other";
                sheet.Cell(3, 4).Value = "다른";
            });

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(1);
            read[0].Node.Should().Be("X.label");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_RowWithEmptyNode_IsSkipped()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                sheet.Cell(1, 1).Value = "Class [Not chosen]";
                sheet.Cell(1, 2).Value = "Node [Not chosen]";
                sheet.Cell(1, 3).Value = "English [Source string]";
                sheet.Cell(1, 4).Value = "Korean (한국어) [Translation]";

                sheet.Cell(2, 1).Value = "ThingDef";
                sheet.Cell(2, 2).Value = "";   // empty node
                sheet.Cell(2, 3).Value = "text";
                sheet.Cell(2, 4).Value = "텍스트";
            });

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_LegacyFallbackHeaders_AreRecognised()
    {
        string path = TempXlsxPath();
        try
        {
            WriteRawXlsx(path, sheet =>
            {
                sheet.Cell(1, 1).Value = "Class [Not chosen]";
                sheet.Cell(1, 2).Value = "Node [Not chosen]";
                sheet.Cell(1, 3).Value = "EN [Source string]";    // legacy fallback
                sheet.Cell(1, 4).Value = "KO [Translation]";      // legacy fallback

                sheet.Cell(2, 1).Value = "ThingDef";
                sheet.Cell(2, 2).Value = "X.label";
                sheet.Cell(2, 3).Value = "Source";
                sheet.Cell(2, 4).Value = "번역";
            });

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(1);
            read[0].Original.Should().Be("Source");
            read[0].Translated.Should().Be("번역");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
