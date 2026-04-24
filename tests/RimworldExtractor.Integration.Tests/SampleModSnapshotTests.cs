using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RimworldExtractor.Application;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Legacy;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Integration.Tests;

public class SampleModSnapshotTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string RepoRoot => typeof(SampleModSnapshotTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(a => a.Key == "RepositoryRoot")
        .Value!;

    private readonly string _tempDir;

    public SampleModSnapshotTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("rwx-phase4-snapshot-").FullName;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task NewPipeline_ExtractsSampleMod_MatchesLegacySnapshot()
    {
        // === Arrange ===
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        var fs = new PhysicalFileSystem();

        // Build AppSettings equivalent to legacy Prefabs.Init() defaults.
        // Reuse LegacyPrefabsReader to load a synthesized Prefabs.dat that mimics the defaults.
        var settings = BuildSettingsMatchingLegacyDefaults();
        var initialStore = new JsonSettingsStore(fs, settingsPath);
        await initialStore.SaveAsync(settings, TestContext.Current.CancellationToken);

        // Build DI
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(settingsPath);
        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        var pipeline = provider.GetRequiredService<IExtractionPipeline>();
        var discovery = provider.GetRequiredService<ModDiscoveryService>();

        var sampleRoot = Path.Combine(RepoRoot, "samples", "sample-mod");
        var mod = discovery.ReadMetadata(sampleRoot)
            ?? throw new InvalidOperationException("Sample mod not discoverable at " + sampleRoot);
        var folders = discovery.GetExtractableFolders(mod);
        folders.Should().NotBeEmpty("sample-mod must have extractable folders");

        var request = new ExtractionRequest(
            Target: mod,
            SelectedFolders: folders,
            ReferenceMods: null,
            Settings: settings);

        // === Act ===
        var result = await pipeline.RunAsync(request, progress: null, TestContext.Current.CancellationToken);

        // === Assert: serialize in same shape as Phase 0 snapshot and byte-compare ===
        var ordered = result.Entries
            .OrderBy(e => e.ClassName, StringComparer.Ordinal)
            .ThenBy(e => e.Node, StringComparer.Ordinal)
            .ThenBy(e => e.Original, StringComparer.Ordinal)
            .Select(e => new
            {
                e.ClassName,
                e.Node,
                e.Original,
                e.SourceFile
            })
            .ToList();

#pragma warning disable IL2026, IL3050 // AOT: test-only code, never published as AOT
        var actualJson = NormalizeLineEndings(JsonSerializer.Serialize(ordered, JsonOptions));
#pragma warning restore IL2026, IL3050

        var snapshotPath = Path.Combine(RepoRoot, "tests", "__snapshots__", "legacy", "sample-mod.extraction.json");
        var expectedJson = NormalizeLineEndings(await File.ReadAllTextAsync(snapshotPath, TestContext.Current.CancellationToken));

        actualJson.Should().Be(expectedJson, "new pipeline output must match legacy baseline byte-for-byte");
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n").Replace("\r", "\n");

    /// <summary>
    /// Constructs an AppSettings that mirrors the legacy Prefabs.Init() defaults:
    /// the large ExtractableTags list, NodeReplacement entries, TranslationHandles, etc.
    /// </summary>
    private AppSettings BuildSettingsMatchingLegacyDefaults()
    {
        // Read legacy Prefabs.cs line 168-170 to get the ExtractableTags string.
        // For the sample mod baseline, not all tags are needed — but we must include enough
        // to match legacy extraction. Key tags: label, description, jobString.

        // Simplest approach: synthesize a Prefabs.dat file matching legacy.Prefabs.Init() output,
        // then run LegacyPrefabsReader on it.
        var legacyPrefabsText = BuildLegacyPrefabsDatContent();
        var tempPrefabs = Path.Combine(_tempDir, "Prefabs.dat");
        File.WriteAllText(tempPrefabs, legacyPrefabsText);
        var settings = LegacyPrefabsReader.Read(tempPrefabs);

        // Override paths to point into the test's repo root
        return settings with
        {
            Paths = settings.Paths with
            {
                Rimworld = RepoRoot,
                Workshop = RepoRoot,
            },
        };
    }

    private static string BuildLegacyPrefabsDatContent()
    {
        // Line-delimited v9 format. Extractable tags include the critical ones for sample-mod:
        // label, description, jobString. Including the full legacy list makes the test
        // robust against extra content appearing in future sample mods.

        const string extractableTagsRaw =
            "label/rulesStrings/description/baseDesc/title/titleShort/customLabel/symbol/" +
            "jobString/reportString/labelNoun/slateRef/verb/gerund/adjective/member/tips/" +
            "ideoName/thoughtStageDescriptions/jobReportString/theme/labelShortAdj/labelPlural/" +
            "letterText/deathMessage/labelShort/letterLabel/helpText/text/baseInspectLine/" +
            "labelFemale/descriptionShort/beginLetter/ingestCommandString/ingestReportString/" +
            "titleShortFemale/titleFemale/gerundLabel/pawnLabel/stageName/shortDescription/" +
            "customEffectDescriptions/endMessage/leaderTitle/pawnSingular/pawnsPlural/desc/" +
            "recoveryMessage/chargeNoun/cooldownGerund/type/potentialExtraOutcomeDesc/" +
            "labelNounPretty/headerTip/rejectInputMessage/spectatorGerund/spectatorsLabel/" +
            "fuelLabel/formatString/useLabel/RMBLabel/permanentLabel/name/missingDesc/" +
            "worshipRoomLabel/labelAbstract/fuelGizmoLabel/destroyedLabel/outOfFuelMessage/" +
            "summary/ritualExpectedDesc/customSummary/meatLabel/labelForFullStatList/tooltip/" +
            "gizmoLabel/onMapInstruction/letterTitle/textEnemy/destroyedOutLabel/beginLetterLabel/" +
            "labelMale/groupName/gizmoDescription/names/arrivalTextEnemy/letterLabelEnemy/" +
            "arrivedLetter/calledOffMessage/finishedMessage/approachingReportString/" +
            "approachOrderString/expectedThingLabelTip/skillLabel/extraPredictedOutcomeDescriptions/" +
            "modNameReadable/descriptionFuture/textWillArrive/arrivalTextFriendly/" +
            "letterLabelFriendly/helpTextController/successfullyRemovedHediffMessage/textFriendly/" +
            "eventLabel/textController/descOverride/shortDescOverride/content/discoveredLetterText/" +
            "discoveredLetterTitle/beginLetterContinue/resourceLabel/message/overrideLabel/" +
            "extraTooltip/offMessage/successMessage/effectDesc/letterInfoText/categoryLabel/" +
            "groupLabel/battleStateLabel/customizationTitle/fixedName/noun/lockedReason/" +
            "descriptionExtra/labelPrefix/labelMechanoids/ingestReportStringEat/failMessage/" +
            "valueFormat/structureLabel/labelSocial/labelInBracketsExtraForHediff/ChooseDesc/" +
            "ChooseLabel/ritualExplanation/resourceDescription/discoverLetterText/countdownLabel/" +
            "inspectString/completedLetterText/completedLetterTitle/leaderDescription/" +
            "formatStringUnfinalized/jobReportOverride/discoverLetterLabel/instantlyPermanentLabel/" +
            "notifyMessage/onCooldownString/invalidTargetPawn/noAssignablePawnsDesc/reportText/" +
            "statLabel/visualLabel/commandDescriptions/successMessageNoNegativeThought/" +
            "tipLabelOverride/mainPartAllThreatsLabel/customChildDisallowMessage/" +
            "ritualExpectedDescNoAdjective/loweredName/cancelLabel/texName/labelOverride/" +
            "messageText/proficiencyAdjective/stuffAdjective/unit/labelTendedWell/" +
            "labelTendedWellInner/labelSolidTendedWell/overrideTooltip/royalFavorLabel/" +
            "extraReportString/spawnInBackstories/customLetterLabel/customLetterText/" +
            "confirmationDialogText/tip/outcomeDescription/generalDescription/generalTitle/" +
            "dialogue/activateDescString/activateLabelString/completedLetter/completedLetterLabel/" +
            "guiLabelString/gizmoDesc/activatedMessageKey/appendString/gizmoDesc1/gizmoDesc2/" +
            "gizmoLabel1/gizmoLabel";

        var lines = new[]
        {
            "DO NOT EDIT THIS MANUALLY",
            "9",                             // version
            "False",                         // enableTkey
            "",                              // pathRimworld (will be overridden)
            "",                              // pathWorkshop (will be overridden)
            "",                              // pathBaseRefList
            "1.6",                           // currentVersion
            @"^[1]\.\d+",                    // patternVersion (dropped)
            @"^v[1]\.\d+",                   // patternVersionWithV (dropped)
            "English",                       // originalLanguage
            "Korean (한국어)",                // translationLanguage
            "False",                         // commentOriginal
            extractableTagsRaw,              // extractableTags
            "rulesFiles/rulesStrings/pathList", // fullListTags
            "",                              // nodeReplacement (empty for sample-mod)
            "*verbClass/*compClass",         // translationHandles
            "Overwrite",                     // policy
            "Languages",                     // method
        };

        return string.Join('\n', lines);
    }
}
