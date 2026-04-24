using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Output;

namespace RimworldExtractor.Infrastructure.Tests.Output;

public class ExcelOutputStrategyTests
{
    private static readonly LanguageCode English = LanguageCode.Create("English");
    private static readonly LanguageCode Korean = LanguageCode.Create("Korean (한국어)");

    private static (ExcelOutputStrategy strategy, string tempDir) CreateStrategy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ExcelStrategyTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var fs = new PhysicalFileSystem();
        var safeWriter = new SafeFileWriter(fs);
        var writer = new ClosedXmlWriter(safeWriter);
        return (new ExcelOutputStrategy(writer), tempDir);
    }

    [Fact]
    public void Format_ReturnsExcel()
    {
        var (strategy, tempDir) = CreateStrategy();
        try
        {
            strategy.Format.Should().Be(ExtractionFormat.Excel);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task WriteAsync_CreatesXlsxFileAtExpectedPath()
    {
        var (strategy, tempDir) = CreateStrategy();
        try
        {
            var translations = new[]
            {
                new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
            };

            await strategy.WriteAsync(
                translations,
                outputRoot: tempDir,
                baseFileName: "output",
                originalLanguage: English,
                translationLanguage: Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            var expectedPath = Path.Combine(tempDir, "output.xlsx");
            File.Exists(expectedPath).Should().BeTrue("the strategy should write to {{root}}/{{base}}.xlsx");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
