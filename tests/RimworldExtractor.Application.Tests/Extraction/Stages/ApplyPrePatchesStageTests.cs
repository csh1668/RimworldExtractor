using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ApplyPrePatchesStageTests
{
    private static readonly ModMetadata TargetMod =
        new("/target", "tid", "TargetMod", "author.target", false);

    private static ExtractionContext MakeContext(XDocument? initialDefs = null)
    {
        var patchFolder = new ExtractableFolder(TargetMod, "Patches", null);
        var request = new ExtractionRequest(
            Target: TargetMod,
            SelectedFolders: new[] { patchFolder },
            ReferenceMods: null,
            Settings: AppSettings.Default);
        var ctx = new ExtractionContext(request);
        if (initialDefs is not null)
        {
            ctx.ReplaceCombinedDefs(initialDefs);
        }

        return ctx;
    }

    [Fact]
    public async Task ExecuteAsync_InPrePatchMode_ProducesNoEntries()
    {
        // In prePatchMode=true, all patch operations are no-ops (yield break in legacy PatchOperations).
        // The stage should run without error, producing no entries.
        var initialDefs = new XDocument(new XElement("Defs",
            new XElement("ThingDef",
                new XElement("defName", "SampleWeapon"),
                new XElement("label", "old label"))));

        var fs = new InMemoryFileSystem();
        fs.AddFile("/target/Patches/patch1.xml", """
            <?xml version="1.0"?>
            <Patch>
                <Operation Class="PatchOperationReplace">
                    <xpath>/Defs/ThingDef[defName="SampleWeapon"]/label</xpath>
                    <value><label>new label</label></value>
                </Operation>
            </Patch>
            """);

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<ApplyPrePatchesStage>.Instance;

        var xmlDefExtractor = new XmlDefExtractor();
        var patchLogger = NullLogger<XPatchProcessor>.Instance;
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, patchLogger);

        var ctx = MakeContext(initialDefs);

        var stage = new ApplyPrePatchesStage(patchProcessor, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(ctx);

        // prePatchMode=true means PatchOperationReplace is a no-op; CombinedDefs unchanged
        var label = ctx.CombinedDefs.Root!
            .Elements("ThingDef")
            .FirstOrDefault()?
            .Element("label")?.Value;
        label.Should().Be("old label", "prePatchMode=true patches do not mutate CombinedDefs");
        // No entries emitted
        ctx.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesParentLookupFromExistingNamedNodes()
    {
        // The stage's post-patch scan updates ParentLookup from all nodes in CombinedDefs.
        // This mirrors legacy DoPrePatch lines 165-172.
        var namedNode = new XElement("ThingDef",
            new XAttribute("Name", "AbstractBase"),
            new XAttribute("Abstract", "True"),
            new XElement("label", "Base"));
        var initialDefs = new XDocument(new XElement("Defs", namedNode));

        var fs = new InMemoryFileSystem();
        // Patch folder exists but is empty
        fs.CreateDirectory("/target/Patches");

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<ApplyPrePatchesStage>.Instance;

        var xmlDefExtractor = new XmlDefExtractor();
        var patchLogger = NullLogger<XPatchProcessor>.Instance;
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, patchLogger);

        var ctx = MakeContext(initialDefs);

        var stage = new ApplyPrePatchesStage(patchProcessor, parser, modLister, gateway, logger);
        await stage.ExecuteAsync(ctx);

        // The Name="AbstractBase" node should be in ParentLookup after the scan
        ctx.ParentLookup.Should().ContainKey("AbstractBase");
        ctx.ParentLookup["AbstractBase"].Name.LocalName.Should().Be("ThingDef");
    }

    [Fact]
    public async Task ExecuteAsync_HandlesEmptyPatchDirectory()
    {
        var initialDefs = new XDocument(new XElement("Defs"));
        var fs = new InMemoryFileSystem();
        fs.CreateDirectory("/target/Patches");

        var gateway = new FileSystemGateway(fs);
        var parser = new XDocumentDefParser(fs);
        var modLister = Substitute.For<IModLister>();
        var logger = NullLogger<ApplyPrePatchesStage>.Instance;

        var xmlDefExtractor = new XmlDefExtractor();
        var patchLogger = NullLogger<XPatchProcessor>.Instance;
        var patchProcessor = new XPatchProcessor(xmlDefExtractor, patchLogger);

        var ctx = MakeContext(initialDefs);

        var stage = new ApplyPrePatchesStage(patchProcessor, parser, modLister, gateway, logger);
        var act = async () => await stage.ExecuteAsync(ctx);
        await act.Should().NotThrowAsync();
        ctx.Results.Should().BeEmpty();
    }
}
