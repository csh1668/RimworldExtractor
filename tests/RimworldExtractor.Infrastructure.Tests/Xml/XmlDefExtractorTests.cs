using System.Xml.Linq;
using FluentAssertions;
using RimworldExtractor.Domain.Rules;
using RimworldExtractor.Infrastructure.Xml;

namespace RimworldExtractor.Infrastructure.Tests.Xml;

/// <summary>
/// Tests for Tasks 17 and 18: XmlDefExtractor (BFS FindExtractableNodes port)
/// + translation-handle matching + XmlExtensions.SettingsMenuDef special case.
/// </summary>
public class XmlDefExtractorTests
{
    private static readonly XmlDefExtractor Extractor = new();

    private static Dictionary<string, ExtractionRule> Rules(params string[] tags)
    {
        return tags.ToDictionary(
            t => t,
            t => new ExtractionRule(t));
    }

    private static readonly IReadOnlyList<TranslationHandle> NoHandles = [];

    // ───────── Task 17: BFS extraction ─────────

    [Fact]
    public void Extract_SimpleLabel_YieldsEntry()
    {
        var root = XElement.Parse("""
            <ThingDef>
              <defName>WoodSpear</defName>
              <label>wood spear</label>
            </ThingDef>
            """);

        var entries = Extractor.Extract(
            "WoodSpear", "ThingDef", root,
            Rules("label"), NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().ContainSingle();
        entries[0].Node.Should().Be("WoodSpear.label");
        entries[0].Original.Should().Be("wood spear");
        entries[0].ClassName.Should().Be("ThingDef");
    }

    [Fact]
    public void Extract_ListItems_YieldNumericIndices()
    {
        var root = XElement.Parse("""
            <ThingDef>
              <defName>SpearList</defName>
              <weaponTags>
                <li>Melee</li>
                <li>Blunt</li>
              </weaponTags>
            </ThingDef>
            """);

        var entries = Extractor.Extract(
            "SpearList", "ThingDef", root,
            Rules("weaponTags"), NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().HaveCount(2);
        entries[0].Node.Should().Be("SpearList.weaponTags.0");
        entries[1].Node.Should().Be("SpearList.weaponTags.1");
    }

    [Fact]
    public void Extract_BlacklistedDef_SkipsEntry()
    {
        var root = XElement.Parse("""
            <ThingDef>
              <defName>BannedDef</defName>
              <label>skip me</label>
            </ThingDef>
            """);

        var rules = new Dictionary<string, ExtractionRule>
        {
            ["label"] = new ExtractionRule("label", blacklist: ["BannedDef"])
        };

        var entries = Extractor.Extract(
            "BannedDef", "ThingDef", root,
            rules, NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().BeEmpty();
    }

    [Fact]
    public void Extract_TKeyEnabled_UsesTKeyAttribute()
    {
        var root = XElement.Parse("""
            <ThingDef>
              <defName>Slate</defName>
              <label TKey="Slate.label.slate">slate stone</label>
            </ThingDef>
            """);

        var entries = Extractor.Extract(
            "Slate", "ThingDef", root,
            Rules("label"), NoHandles,
            enableTKey: true, isOfficialContent: false).ToList();

        entries.Should().ContainSingle();
        entries[0].Node.Should().Be("Slate.Slate.label.slate.slateRef");
    }

    [Fact]
    public void Extract_IsOfficialContent_IncludesSourceFile()
    {
        var root = XElement.Parse("""
            <ThingDef SourceFile="Core/Defs/Things/Spear.xml">
              <defName>CoreSpear</defName>
              <label>core spear</label>
            </ThingDef>
            """);

        var entries = Extractor.Extract(
            "CoreSpear", "ThingDef", root,
            Rules("label"), NoHandles,
            enableTKey: false, isOfficialContent: true).ToList();

        entries.Should().ContainSingle();
        entries[0].SourceFile.Should().Be("Core/Defs/Things/Spear.xml");
    }

    // ───────── Task 18: Translation-handle matching ─────────

    [Fact]
    public void Extract_WithTranslationHandle_UsesNormalizedHandleAsSegment()
    {
        // The <li> node contains a <defName> child whose value is used as the path segment
        var root = XElement.Parse("""
            <ThingDef>
              <defName>ParentDef</defName>
              <comps>
                <li>
                  <defName>CompA</defName>
                  <label>component A</label>
                </li>
              </comps>
            </ThingDef>
            """);

        var handles = new List<TranslationHandle> { TranslationHandle.Parse("defName") };

        var entries = Extractor.Extract(
            "ParentDef", "ThingDef", root,
            Rules("label"), handles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().ContainSingle();
        // Path segment should be normalized handle value "CompA" not numeric index
        entries[0].Node.Should().Be("ParentDef.comps.CompA.label");
    }

    [Fact]
    public void Extract_WithWildcardHandle_UsesSplitLastAsSegment()
    {
        // Wildcard (*) handle uses .Split('.').Last() on the inner text
        var root = XElement.Parse("""
            <ThingDef>
              <defName>ParentDef</defName>
              <comps>
                <li>
                  <Class>Verse.CompProperties_Explosive</Class>
                  <label>explosive comp</label>
                </li>
              </comps>
            </ThingDef>
            """);

        var handles = new List<TranslationHandle> { TranslationHandle.Parse("*Class") };

        var entries = Extractor.Extract(
            "ParentDef", "ThingDef", root,
            Rules("label"), handles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().ContainSingle();
        // Split('.').Last() of "Verse.CompProperties_Explosive" = "CompProperties_Explosive"
        entries[0].Node.Should().Be("ParentDef.comps.CompProperties_Explosive.label");
    }

    // ───────── Task 18: XmlExtensions.SettingsMenuDef special case ─────────

    [Fact]
    public void Extract_XmlExtensionsSettingsMenuDef_WithTKey_YieldsKeyedEntry()
    {
        var root = XElement.Parse("""
            <SettingsMenuDef>
              <defName>MySettings</defName>
              <settings>
                <li>
                  <tKey>MyMod.Settings.MyLabel</tKey>
                  <label>My label text</label>
                </li>
              </settings>
            </SettingsMenuDef>
            """);

        var entries = Extractor.Extract(
            "MySettings", "XmlExtensions.SettingsMenuDef", root,
            new Dictionary<string, ExtractionRule>(), NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().ContainSingle(e => e.ClassName == "Keyed");
        var entry = entries.Single();
        entry.Node.Should().Be("MyMod.Settings.MyLabel");
        entry.Original.Should().Be("My label text");
    }

    [Fact]
    public void Extract_XmlExtensionsSettingsMenuDef_WithTKeyTip_YieldsKeyedEntryForTooltip()
    {
        var root = XElement.Parse("""
            <SettingsMenuDef>
              <defName>MySettings</defName>
              <settings>
                <li>
                  <tKey>MyMod.Settings.MyLabel</tKey>
                  <tKeyTip>MyMod.Settings.MyLabelTip</tKeyTip>
                  <label>Label</label>
                  <tooltip>Tooltip text</tooltip>
                </li>
              </settings>
            </SettingsMenuDef>
            """);

        var entries = Extractor.Extract(
            "MySettings", "XmlExtensions.SettingsMenuDef", root,
            new Dictionary<string, ExtractionRule>(), NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().HaveCount(2);
        entries.Should().Contain(e => e.ClassName == "Keyed" && e.Node == "MyMod.Settings.MyLabel" && e.Original == "Label");
        entries.Should().Contain(e => e.ClassName == "Keyed" && e.Node == "MyMod.Settings.MyLabelTip" && e.Original == "Tooltip text");
    }

    [Fact]
    public void Extract_XmlExtensionsSettingsMenuDef_WithoutTKey_YieldsDefEntryWithFullPath()
    {
        var root = XElement.Parse("""
            <SettingsMenuDef>
              <defName>MySettings</defName>
              <settings>
                <li>
                  <label>No key label</label>
                </li>
              </settings>
            </SettingsMenuDef>
            """);

        var entries = Extractor.Extract(
            "MySettings", "XmlExtensions.SettingsMenuDef", root,
            new Dictionary<string, ExtractionRule>(), NoHandles,
            enableTKey: false, isOfficialContent: false).ToList();

        entries.Should().ContainSingle();
        entries[0].ClassName.Should().Be("XmlExtensions.SettingsMenuDef");
        entries[0].Node.Should().StartWith("MySettings.");
    }
}
