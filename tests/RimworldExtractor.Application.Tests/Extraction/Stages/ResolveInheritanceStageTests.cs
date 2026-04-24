using System.Xml.Linq;
using FluentAssertions;
using NSubstitute;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Application.Tests.Extraction.Stages;

public class ResolveInheritanceStageTests
{
    private static ExtractionContext MakeContext(XDocument? defs = null)
    {
        var mod = new ModMetadata("/mod", "id", "Mod", "author.mod", false);
        var request = new ExtractionRequest(
            Target: mod,
            SelectedFolders: Array.Empty<ExtractableFolder>(),
            ReferenceMods: null,
            Settings: AppSettings.Default);
        var ctx = new ExtractionContext(request);
        if (defs is not null) ctx.ReplaceCombinedDefs(defs);
        return ctx;
    }

    [Fact]
    public void ExecuteAsync_CallsResolverAndReplacesCombinedDefs()
    {
        var inputDefs = new XDocument(new XElement("Defs"));
        var resolvedDefs = new XDocument(new XElement("Defs", new XElement("ThingDef")));

        var resolver = Substitute.For<IXmlInheritanceResolver>();
        resolver.Resolve(inputDefs).Returns(resolvedDefs);

        var ctx = MakeContext(inputDefs);
        var stage = new ResolveInheritanceStage(resolver);

        stage.ExecuteAsync(ctx);

        ctx.CombinedDefs.Should().BeSameAs(resolvedDefs);
        resolver.Received(1).Resolve(inputDefs);
    }

    [Fact]
    public void ExecuteAsync_WithRealResolver_ResolvesInheritance()
    {
        // Parent: AbstractBase with label "Base Label"
        // Child: ConcreteThing ParentName="AbstractBase", defName="Sword"
        // Expected: ConcreteThing has label "Base Label" after resolution
        var defs = new XDocument(new XElement("Defs",
            new XElement("ThingDef",
                new XAttribute("Name", "AbstractBase"),
                new XAttribute("Abstract", "True"),
                new XElement("label", "Base Label")),
            new XElement("ThingDef",
                new XAttribute("ParentName", "AbstractBase"),
                new XElement("defName", "Sword"))));

        var resolver = new XmlInheritanceResolver();
        var ctx = MakeContext(defs);

        var stage = new ResolveInheritanceStage(resolver);
        stage.ExecuteAsync(ctx);

        var sword = ctx.CombinedDefs.Root!.Elements("ThingDef")
            .FirstOrDefault(e => e.Element("defName")?.Value == "Sword");
        sword.Should().NotBeNull("Sword should survive inheritance resolution");
        sword!.Element("label")?.Value.Should().Be("Base Label",
            "Sword should inherit label from AbstractBase");
    }
}
