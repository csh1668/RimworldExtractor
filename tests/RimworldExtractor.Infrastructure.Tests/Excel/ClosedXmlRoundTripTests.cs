using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.Excel;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.Excel;

/// <summary>
/// Integration tests that exercise the full write → read round-trip via
/// <see cref="ClosedXmlWriter"/> and <see cref="ClosedXmlReader"/>.
/// </summary>
public class ClosedXmlRoundTripTests
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
        string dir = Path.Combine(Path.GetTempPath(), $"RoundTrip_{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "roundtrip.xlsx");
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RoundTrip_VariedEntries_PreservesAllFields()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef",   "SteelDef.label",          "Steel",     "강철",     null, null),
                new TranslationEntry("ThingDef",   "SteelDef.description",     "Cold metal", "차가운 금속", null, null),
                new TranslationEntry("Keyed",      "UI.SaveButton",            "Save",      null,       null, null),
                new TranslationEntry("Patches.Mod","PatchedDef.label",         "Patched",   "패치됨",   null, null),
                new TranslationEntry("Strings",    "Names/First/Western.0",    "Alice",     "앨리스",   null, null),
            };

            await CreateWriter().WriteAsync(entries, path, English, Korean,
                markNoTranslation: true,
                cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                TranslationEntry expected = entries[i];
                TranslationEntry actual = read[i];

                actual.ClassName.Should().Be(expected.ClassName,
                    because: $"entry [{i}] ClassName must survive round-trip");
                actual.Node.Should().Be(expected.Node,
                    because: $"entry [{i}] Node must survive round-trip");
                actual.Original.Should().Be(expected.Original,
                    because: $"entry [{i}] Original must survive round-trip");
                actual.Translated.Should().Be(expected.Translated,
                    because: $"entry [{i}] Translated must survive round-trip");
                // RequiredMods parsing is deferred; we do not assert on it.
            }
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task RoundTrip_EmptyTranslation_RoundTripsAsNull()
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
    public async Task RoundTrip_SpecialCharacters_ArePreserved()
    {
        string path = TempXlsxPath();
        try
        {
            var entries = new[]
            {
                new TranslationEntry("ThingDef", "A.label", "Text with \"quotes\" & <brackets>", "특수문자 \"따옴표\"", null, null),
                new TranslationEntry("ThingDef", "B.label", "Line1\nLine2", "줄1\n줄2", null, null),
            };

            await CreateWriter().WriteAsync(entries, path, English, Korean,
                cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<TranslationEntry> read = await CreateReader().ReadAsync(path,
                TestContext.Current.CancellationToken);

            read.Should().HaveCount(2);
            read[0].Original.Should().Be(entries[0].Original);
            read[0].Translated.Should().Be(entries[0].Translated);
            read[1].Original.Should().Be(entries[1].Original);
            read[1].Translated.Should().Be(entries[1].Translated);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
