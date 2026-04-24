using FluentAssertions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Output;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Output;

public class LanguagesOutputStrategyTests
{
    private static readonly LanguageCode Korean = LanguageCode.Create("Korean (한국어)");
    private static readonly LanguageCode English = LanguageCode.Create("English");
    private const string RootDir = "/workspace";

    private static (LanguagesOutputStrategy plain, LanguagesWithCommentsOutputStrategy withComments, InMemoryFileSystem fs)
        BuildStrategies()
    {
        var fs = new InMemoryFileSystem();
        var safeWriter = new SafeFileWriter(fs);
        var writer = new XmlLanguagesWriter(safeWriter, fs);
        return (new LanguagesOutputStrategy(writer), new LanguagesWithCommentsOutputStrategy(writer), fs);
    }

    /// <summary>Recursively enumerates all files under <paramref name="root"/>.</summary>
    private static IEnumerable<string> EnumerateAllFiles(InMemoryFileSystem fs, string root)
    {
        foreach (var file in fs.EnumerateFiles(root))
            yield return file;
        foreach (var dir in fs.EnumerateDirectories(root))
        foreach (var file in EnumerateAllFiles(fs, dir))
            yield return file;
    }

    [Fact]
    public void LanguagesOutputStrategy_Format_ReturnsLanguages()
    {
        var (plain, _, _) = BuildStrategies();
        plain.Format.Should().Be(ExtractionFormat.Languages);
    }

    [Fact]
    public void LanguagesWithCommentsOutputStrategy_Format_ReturnsLanguagesWithComments()
    {
        var (_, withComments, _) = BuildStrategies();
        withComments.Format.Should().Be(ExtractionFormat.LanguagesWithComments);
    }

    [Fact]
    public async Task LanguagesOutputStrategy_WriteAsync_ProducesDefInjectedFiles()
    {
        var (plain, _, fs) = BuildStrategies();
        var ct = TestContext.Current.CancellationToken;

        var translations = new[]
        {
            new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
        };

        await plain.WriteAsync(translations, RootDir, "myfile", English, Korean, ct);

        // DefInjected directory should contain at least one file
        var files = EnumerateAllFiles(fs, RootDir).ToList();
        files.Should().NotBeEmpty("writing via LanguagesOutputStrategy should produce XML files");
        files.Should().Contain(f => f.Contains("DefInjected"), "DefInjected entries should be written");
    }

    [Fact]
    public async Task LanguagesWithCommentsOutputStrategy_WriteAsync_ProducesCommentedFiles()
    {
        var (_, withComments, fs) = BuildStrategies();
        var ct = TestContext.Current.CancellationToken;

        var translations = new[]
        {
            new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
        };

        await withComments.WriteAsync(translations, RootDir, "myfile", English, Korean, ct);

        // Read the file content and check for Original= comment
        var files = EnumerateAllFiles(fs, RootDir).ToList();
        files.Should().NotBeEmpty();

        var defInjectedFile = files.FirstOrDefault(f => f.Contains("DefInjected"));
        defInjectedFile.Should().NotBeNull();

        var content = await fs.ReadAllTextAsync(defInjectedFile!, ct);
        content.Should().Contain("Original=", "commentOriginal=true should emit Original= comments");
    }
}
