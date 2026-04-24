using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

/// <summary>
/// Task 30: Round-trip integration tests.
/// Uses <see cref="InMemoryFileSystem"/> — no disk I/O.
/// </summary>
public class XmlLanguagesRoundTripTests
{
    private static readonly LanguageCode Korean = LanguageCode.Create("Korean (한국어)");
    private const string RootDir = "/workspace";
    private const string FileName = "translations";
    private static readonly IReadOnlyList<string> NoFullListTags = [];
    private static readonly IReadOnlyList<string> FullListTags = ["rulesStrings"];

    private static (XmlLanguagesWriter writer, XmlLanguagesReader reader, InMemoryFileSystem fs) BuildSut()
    {
        var fs = new InMemoryFileSystem();
        var safeWriter = new SafeFileWriter(fs);
        var writer = new XmlLanguagesWriter(safeWriter, fs);
        var reader = new XmlLanguagesReader(fs);
        return (writer, reader, fs);
    }

    // ─── DefInjected round-trip ─────────────────────────────────────────────────

    [Fact]
    public async Task DefInjected_RoundTrip_PreservesNodeAndTranslated()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
            new TranslationEntry("ThingDef", "WoodLog.description", "A log of wood.", "통나무.", null, null),
            new TranslationEntry("PawnKindDef", "Muffalo.label", "muffalo", "머펄로", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        output.Should().HaveCountGreaterThanOrEqualTo(3);

        var thingDefs = output.Where(e => e.ClassName == "ThingDef").ToList();
        thingDefs.Should().Contain(e => e.Node == "WoodLog.label" && e.Translated == "원목");
        thingDefs.Should().Contain(e => e.Node == "WoodLog.description" && e.Translated == "통나무.");

        var pawnKinds = output.Where(e => e.ClassName == "PawnKindDef").ToList();
        pawnKinds.Should().Contain(e => e.Node == "Muffalo.label" && e.Translated == "머펄로");
    }

    // ─── SkipNoTranslation ──────────────────────────────────────────────────────

    [Fact]
    public async Task DefInjected_SkipNoTranslation_OmitsUntranslated()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
            new TranslationEntry("ThingDef", "WoodLog.description", "A log.", null, null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: true, commentOriginal: false, NoFullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        output.Should().ContainSingle(e => e.ClassName == "ThingDef" && e.Node == "WoodLog.label");
        output.Should().NotContain(e => e.Node == "WoodLog.description");
    }

    // ─── Keyed round-trip ───────────────────────────────────────────────────────

    [Fact]
    public async Task Keyed_RoundTrip_PreservesNodeAndTranslated()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("Keyed", "LetterLabelRecruitSuccess", "Recruit success", "징집 성공", null, null),
            new TranslationEntry("Keyed", "JobFailReasonIncompatibleLord", "Incompatible lord", "호환되지 않음", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        var keyedOut = output.Where(e => e.ClassName == "Keyed").ToList();
        keyedOut.Should().Contain(e => e.Node == "LetterLabelRecruitSuccess" && e.Translated == "징집 성공");
        keyedOut.Should().Contain(e => e.Node == "JobFailReasonIncompatibleLord" && e.Translated == "호환되지 않음");
    }

    // ─── Strings round-trip ─────────────────────────────────────────────────────

    [Fact]
    public async Task Strings_RoundTrip_PreservesLines()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("Strings", "Names.Korean.FirstNames.김철수", string.Empty, "김철수", null, null),
            new TranslationEntry("Strings", "Names.Korean.FirstNames.이영희", string.Empty, "이영희", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        var stringsOut = output.Where(e => e.ClassName == "Strings").ToList();
        stringsOut.Should().Contain(e => e.Translated == "김철수");
        stringsOut.Should().Contain(e => e.Translated == "이영희");
    }

    // ─── FullListTranslation collapse ───────────────────────────────────────────

    [Fact]
    public async Task FullListTranslation_CollapsesIndexedNodes_IntoLiElements()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("GrammarRuleDef", "MyRule.rulesStrings.0", "rule0", "규칙0", null, null),
            new TranslationEntry("GrammarRuleDef", "MyRule.rulesStrings.1", "rule1", "규칙1", null, null),
            new TranslationEntry("GrammarRuleDef", "MyRule.rulesStrings.2", "rule2", "규칙2", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, FullListTags, ct);

        // FullList entries go to a numbered file (translations-00.xml)
        var xmlPath = $"{RootDir}/Languages/Korean/DefInjected/GrammarRuleDef/{FileName}-00.xml";
        var xmlText = await fs.ReadAllTextAsync(xmlPath, ct);
        // The consolidated element is named after the parent path (MyRule.rulesStrings)
        xmlText.Should().Contain("<li>규칙0</li>");
        xmlText.Should().Contain("<li>규칙1</li>");
        xmlText.Should().Contain("<li>규칙2</li>");
        // Indexed form should be gone
        xmlText.Should().NotContain("rulesStrings.0");
    }

    // ─── FullListTranslation round-trip ─────────────────────────────────────────

    [Fact]
    public async Task FullListTranslation_RoundTrip_RecoversIndexedEntries()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("GrammarRuleDef", "MyRule.rulesStrings.0", "rule0", "규칙0", null, null),
            new TranslationEntry("GrammarRuleDef", "MyRule.rulesStrings.1", "rule1", "규칙1", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, FullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        // After FullListTranslation collapse + read-back, the parent name is "MyRule.rulesStrings"
        // and children are indexed as MyRule.rulesStrings.0, MyRule.rulesStrings.1
        var grammarOut = output.Where(e => e.ClassName == "GrammarRuleDef").ToList();
        grammarOut.Should().Contain(e => e.Node == "MyRule.rulesStrings.0" && e.Translated == "규칙0");
        grammarOut.Should().Contain(e => e.Node == "MyRule.rulesStrings.1" && e.Translated == "규칙1");
    }

    // ─── Interpolation {*ClassName+Node} ────────────────────────────────────────

    [Fact]
    public async Task Interpolation_ResolvesPointerTokens()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "Iron.label", "iron", "철", null, null),
            new TranslationEntry("ThingDef", "SteelBar.description", "made of iron", "철({*ThingDef+Iron.label})로 만듦", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var xmlPath = $"{RootDir}/Languages/Korean/DefInjected/ThingDef/{FileName}.xml";
        var xmlText = await fs.ReadAllTextAsync(xmlPath, ct);

        xmlText.Should().Contain("철(철)로 만듦");
    }

    // ─── CommentOriginal ────────────────────────────────────────────────────────

    [Fact]
    public async Task CommentOriginal_EmitsXmlComments()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "WoodLog.label", "wood log", "원목", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: true, NoFullListTags, ct);

        var xmlPath = $"{RootDir}/Languages/Korean/DefInjected/ThingDef/{FileName}.xml";
        var xmlText = await fs.ReadAllTextAsync(xmlPath, ct);

        xmlText.Should().Contain("<!--");
        xmlText.Should().Contain("wood log");
    }

    // ─── Patches output ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Patches_WritesXmlFile()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("Patches.ThingDef", "WoodLog.label", "wood log", "원목", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var patchPath = $"{RootDir}/Patches/{FileName}.xml";
        fs.FileExists(patchPath).Should().BeTrue();

        var xmlText = await fs.ReadAllTextAsync(patchPath, ct);
        xmlText.Should().Contain("<Patch");
        xmlText.Should().Contain("PatchOperationReplace");
        xmlText.Should().Contain("원목");
    }

    // ─── Patches with RequiredMods ───────────────────────────────────────────────

    [Fact]
    public async Task Patches_WithRequiredMods_EmitsFindModWrappers()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var rm = new RequiredMods(
            allowed: [[ModReference.ByModName("Mod A")]],
            disallowed: []);

        var input = new[]
        {
            new TranslationEntry("Patches.ThingDef", "Widget.label", "widget", "위젯", rm, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var patchPath = $"{RootDir}/Patches/{FileName}.xml";
        var xmlText = await fs.ReadAllTextAsync(patchPath, ct);

        xmlText.Should().Contain("PatchOperationFindMod");
        xmlText.Should().Contain("<li>Mod A</li>");
        xmlText.Should().Contain("PatchOperationSequence");
        xmlText.Should().Contain("위젯");
    }

    // ─── Mixed entry types ───────────────────────────────────────────────────────

    [Fact]
    public async Task MixedEntries_AllTypesWrittenAndRead()
    {
        var (writer, reader, _) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "Iron.label", "iron", "철", null, null),
            new TranslationEntry("Keyed", "Accept", "Accept", "수락", null, null),
            new TranslationEntry("Strings", "Names.Korean.Names.홍길동", string.Empty, "홍길동", null, null),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        var output = await reader.ReadAsync(RootDir, Korean, ct);

        output.Should().Contain(e => e.ClassName == "ThingDef" && e.Node == "Iron.label" && e.Translated == "철");
        output.Should().Contain(e => e.ClassName == "Keyed" && e.Node == "Accept" && e.Translated == "수락");
        output.Should().Contain(e => e.ClassName == "Strings" && e.Translated == "홍길동");
    }

    // ─── Official (SourceFile) grouping ─────────────────────────────────────────

    [Fact]
    public async Task Official_DefInjected_GroupsBySourceFile()
    {
        var (writer, _, fs) = BuildSut();
        var ct = TestContext.Current.CancellationToken;

        var input = new[]
        {
            new TranslationEntry("ThingDef", "Iron.label", "iron", "철", null, "Things/Resource/Iron"),
            new TranslationEntry("ThingDef", "Steel.label", "steel", "강철", null, "Things/Resource/Steel"),
        };

        await writer.WriteAsync(input, RootDir, Korean, FileName,
            skipNoTranslation: false, commentOriginal: false, NoFullListTags, ct);

        fs.FileExists($"{RootDir}/Languages/Korean/DefInjected/ThingDef/Things/Resource/Iron.xml")
            .Should().BeTrue();
        fs.FileExists($"{RootDir}/Languages/Korean/DefInjected/ThingDef/Things/Resource/Steel.xml")
            .Should().BeTrue();
    }
}
