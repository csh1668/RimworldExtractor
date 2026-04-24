using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

/// <summary>
/// Tests for Tasks 19-25: IXPatchProcessor dispatch scaffold + 7 operation handlers.
/// </summary>
public class XPatchProcessorTests
{
    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static readonly IReadOnlyList<TranslationHandle> NoHandles = [];

    private static Dictionary<string, ExtractionRule> Rules(params string[] tags) =>
        tags.ToDictionary(t => t, t => new ExtractionRule(t));

    /// <summary>
    /// Build a combined-defs XDocument from raw Def XML strings.
    /// </summary>
    private static XDocument BuildDefs(params string[] defXmls)
    {
        var defs = new XElement("Defs");
        foreach (var xml in defXmls)
            defs.Add(XElement.Parse(xml));
        return new XDocument(defs);
    }

    /// <summary>
    /// Build a &lt;Patch&gt; element containing one &lt;Operation&gt; element.
    /// </summary>
    private static XElement BuildPatch(string operationXml)
    {
        var patch = new XElement("Patch");
        patch.Add(XElement.Parse(operationXml));
        return patch;
    }

    private static XPatchProcessor MakeProcessor()
    {
        var extractor = new XmlDefExtractor();
        var logger = NullLogger<XPatchProcessor>.Instance;
        return new XPatchProcessor(extractor, logger);
    }

    // ──────────────────────────────────────────────
    // Task 19: Scaffold
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_EmptyPatch_ReturnsEmptyResult()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("<ThingDef><defName>Wood</defName><label>wood</label></ThingDef>");
        var patchRoot = new XElement("Patch");

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().BeEmpty();
        result.DefsAddedByPatches.Should().BeEmpty();
    }

    [Fact]
    public void Apply_UnknownOperationClass_ReturnsEmptyResult()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("<ThingDef><defName>Wood</defName><label>wood</label></ThingDef>");
        var patchRoot = BuildPatch("""
            <Operation Class="SomeUnknown.Op">
              <xpath>Defs/ThingDef[defName="Wood"]/label</xpath>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 20: PatchOperationAdd
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_Add_NumericFieldOnly_YieldsNoEntries()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><marketValue>5</marketValue></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Apply_Add_ExtractableLabel_YieldsPatchesEntry()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><label>patched wood</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ClassName.Should().Be("Patches.ThingDef");
        result.Entries[0].Original.Should().Be("patched wood");
    }

    [Fact]
    public void Apply_Add_XpathDefs_AddsToDefsAdded()
    {
        var processor = MakeProcessor();
        var defs = new XDocument(new XElement("Defs"));
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs</xpath>
              <value><ThingDef><defName>NewDef</defName><label>new thing</label></ThingDef></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.DefsAddedByPatches.Should().HaveCount(1);
        result.DefsAddedByPatches[0].Element("defName")?.Value.Should().Be("NewDef");
        result.Entries.Should().BeEmpty(); // DefsAdded — not yielded directly
    }

    [Fact]
    public void Apply_Add_KeyedEntry_YieldsKeyedClassWithoutPatchesPrefix()
    {
        var processor = MakeProcessor();
        // Keyed xpath targets a keyed container; we simulate by having a node where className == "Keyed"
        // In practice, the extractor returns ClassName="Keyed" for XmlExtensions or keyed nodes.
        // We use a substituted extractor to simulate this case.
        var mockExtractor = Substitute.For<IXmlDefExtractor>();
        mockExtractor.Extract(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<XElement>(),
            Arg.Any<IReadOnlyDictionary<string, ExtractionRule>>(),
            Arg.Any<IReadOnlyList<TranslationHandle>>(),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns([new TranslationEntry("Keyed", "MyKey", "hello", null, null, null)]);

        var processorWithMock = new XPatchProcessor(mockExtractor, NullLogger<XPatchProcessor>.Instance);
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><label>x</label></value>
            </Operation>
            """);

        var result = processorWithMock.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ClassName.Should().Be("Keyed"); // No Patches. prefix
    }

    [Fact]
    public void Apply_Add_ExtractableLabel_NodePathIsCorrect()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><label>patched wood</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].Node.Should().Be("Wood.label");
    }

    [Fact]
    public void Apply_Add_PrePatchMode_YieldsNothing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><label>patched</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false, prePatchMode: true);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 21: PatchOperationReplace
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_Replace_Label_YieldsNewTextWithPatchesPrefix()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><label>old wood</label></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationReplace">
              <xpath>Defs/ThingDef[defName="Wood"]/label</xpath>
              <value><label>new wood</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ClassName.Should().Be("Patches.ThingDef");
        result.Entries[0].Original.Should().Be("new wood");
    }

    [Fact]
    public void Apply_Replace_RemovesOriginalNode()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><label>old</label></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationReplace">
              <xpath>Defs/ThingDef[defName="Wood"]/label</xpath>
              <value><label>new</label></value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        // The Defs tree should still have exactly one <label> (the replacement)
        var labels = defs.Root!.Descendants("label").ToList();
        labels.Should().HaveCount(1);
        labels[0].Value.Should().Be("new");
    }

    [Fact]
    public void Apply_Replace_PrePatchMode_YieldsNothing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><label>old</label></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationReplace">
              <xpath>Defs/ThingDef[defName="Wood"]/label</xpath>
              <value><label>new</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false, prePatchMode: true);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 22: PatchOperationInsert
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_Insert_Label_YieldsPatchesEntry()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><marketValue>5</marketValue></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationInsert">
              <xpath>Defs/ThingDef[defName="Wood"]/marketValue</xpath>
              <value><label>inserted wood</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ClassName.Should().Be("Patches.ThingDef");
        result.Entries[0].Original.Should().Be("inserted wood");
    }

    [Fact]
    public void Apply_Insert_InsertsAfterSelectedNode()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><marketValue>5</marketValue></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationInsert">
              <xpath>Defs/ThingDef[defName="Wood"]/marketValue</xpath>
              <value><label>inserted</label></value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        // label should appear after marketValue
        var thingDef = defs.Root!.Element("ThingDef")!;
        var children = thingDef.Elements().ToList();
        var mvIdx = children.FindIndex(e => e.Name.LocalName == "marketValue");
        var lblIdx = children.FindIndex(e => e.Name.LocalName == "label");
        lblIdx.Should().Be(mvIdx + 1);
    }

    [Fact]
    public void Apply_Insert_PrePatchMode_YieldsNothing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName><marketValue>5</marketValue></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationInsert">
              <xpath>Defs/ThingDef[defName="Wood"]/marketValue</xpath>
              <value><label>inserted</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false, prePatchMode: true);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 23: PatchOperationSequence
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_Sequence_TwoAddOps_YieldsCombinedEntries()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationSequence">
              <operations>
                <li Class="PatchOperationAdd">
                  <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                  <value><label>first label</label></value>
                </li>
                <li Class="PatchOperationAdd">
                  <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                  <value><description>some desc</description></value>
                </li>
              </operations>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label", "description"), NoHandles, false, false);

        result.Entries.Should().HaveCount(2);
        result.Entries.Select(e => e.Original).Should().BeEquivalentTo(["first label", "some desc"]);
    }

    [Fact]
    public void Apply_Sequence_Empty_YieldsNothing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationSequence">
              <operations></operations>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 24: PatchOperationFindMod + PatchOperationAddModExtension
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_FindMod_MatchBranch_SetsRequiredModsAllowed()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationFindMod">
              <mods><li>SomeMod</li></mods>
              <match Class="PatchOperationAdd">
                <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                <value><label>match label</label></value>
              </match>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].RequiredMods.Should().NotBeNull();
        result.Entries[0].RequiredMods!.Allowed.Should().HaveCount(1);
        result.Entries[0].RequiredMods!.Allowed[0].Should().ContainSingle(r => r.Value == "SomeMod");
    }

    [Fact]
    public void Apply_FindMod_NoMatchBranch_SetsRequiredModsDisallowed()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationFindMod">
              <mods><li>SomeMod</li></mods>
              <nomatch Class="PatchOperationAdd">
                <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                <value><label>no match label</label></value>
              </nomatch>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].RequiredMods.Should().NotBeNull();
        result.Entries[0].RequiredMods!.Disallowed.Should().HaveCount(1);
        result.Entries[0].RequiredMods!.Disallowed[0].Should().ContainSingle(r => r.Value == "SomeMod");
    }

    [Fact]
    public void Apply_FindMod_BothBranches_YieldsBothEntries()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationFindMod">
              <mods><li>SomeMod</li></mods>
              <match Class="PatchOperationAdd">
                <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                <value><label>match label</label></value>
              </match>
              <nomatch Class="PatchOperationAdd">
                <xpath>Defs/ThingDef[defName="Wood"]</xpath>
                <value><description>nomatch desc</description></value>
              </nomatch>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label", "description"), NoHandles, false, false);

        result.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_AddModExtension_YieldsPatchesEntry()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAddModExtension">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value>
                <li Class="SomeMod.SomeExtension">
                  <label>extension label</label>
                </li>
              </value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].ClassName.Should().Be("Patches.ThingDef");
        result.Entries[0].Original.Should().Be("extension label");
    }

    [Fact]
    public void Apply_AddModExtension_CreatesModExtensionsNodeIfMissing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAddModExtension">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value>
                <li Class="MyExt"><label>x</label></li>
              </value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        var thingDef = defs.Root!.Element("ThingDef")!;
        thingDef.Element("modExtensions").Should().NotBeNull();
    }

    [Fact]
    public void Apply_AddModExtension_PrePatchMode_YieldsNothing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAddModExtension">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><li Class="MyExt"><label>x</label></li></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false, prePatchMode: true);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // Task 25: PatchOperationAttribute Add/Remove/Set
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_AttributeAdd_AddsAttributeWhenMissing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
              <value>true</value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        defs.Root!.Element("ThingDef")!.Attribute("Abstract")?.Value.Should().Be("true");
    }

    [Fact]
    public void Apply_AttributeAdd_DoesNotOverwriteExisting()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef Abstract="false"><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
              <value>true</value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        defs.Root!.Element("ThingDef")!.Attribute("Abstract")?.Value.Should().Be("false");
    }

    [Fact]
    public void Apply_AttributeRemove_RemovesAttribute()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef Abstract="true"><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeRemove">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        defs.Root!.Element("ThingDef")!.Attribute("Abstract").Should().BeNull();
    }

    [Fact]
    public void Apply_AttributeSet_UpdatesExistingAttribute()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef Abstract="true"><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeSet">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
              <value>false</value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        defs.Root!.Element("ThingDef")!.Attribute("Abstract")?.Value.Should().Be("false");
    }

    [Fact]
    public void Apply_AttributeSet_SetsWhenMissing()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeSet">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
              <value>true</value>
            </Operation>
            """);

        processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        defs.Root!.Element("ThingDef")!.Attribute("Abstract")?.Value.Should().Be("true");
    }

    [Fact]
    public void Apply_AttributeOps_YieldNoTranslationEntries()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAttributeAdd">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <attribute>Abstract</attribute>
              <value>true</value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // MayRequire attribute
    // ──────────────────────────────────────────────

    [Fact]
    public void Apply_MayRequireAttribute_SetsRequiredModsOnEntries()
    {
        var processor = MakeProcessor();
        var defs = BuildDefs("""<ThingDef><defName>Wood</defName></ThingDef>""");
        var patchRoot = BuildPatch("""
            <Operation Class="PatchOperationAdd" MayRequire="some.package.id">
              <xpath>Defs/ThingDef[defName="Wood"]</xpath>
              <value><label>wood</label></value>
            </Operation>
            """);

        var result = processor.Apply(defs, patchRoot, Rules("label"), NoHandles, false, false);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].RequiredMods.Should().NotBeNull();
        result.Entries[0].RequiredMods!.Allowed.Should().HaveCount(1);
        result.Entries[0].RequiredMods!.Allowed[0]
            .Should().ContainSingle(r => r.Value == "some.package.id" && r.Kind == ModReferenceKind.PackageId);
    }
}
