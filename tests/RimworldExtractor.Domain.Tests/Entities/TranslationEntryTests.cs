using FluentAssertions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Entities;

public class TranslationEntryTests
{
    [Fact]
    public void Ctor_StoresAllFields()
    {
        var entry = new TranslationEntry(
            ClassName: "ThingDef",
            Node: "SampleMod_Weapon.label",
            Original: "sword",
            Translated: "검",
            RequiredMods: RequiredMods.Empty,
            SourceFile: "Defs/Weapons.xml");

        entry.ClassName.Should().Be("ThingDef");
        entry.Node.Should().Be("SampleMod_Weapon.label");
        entry.Original.Should().Be("sword");
        entry.Translated.Should().Be("검");
        entry.RequiredMods.Should().BeSameAs(RequiredMods.Empty);
        entry.SourceFile.Should().Be("Defs/Weapons.xml");
    }

    [Fact]
    public void ClassNode_JoinsClassAndNodeWithPlus()
    {
        var entry = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);

        entry.ClassNode.Should().Be("ThingDef+Foo.label");
    }

    [Theory]
    [InlineData("Foo.label", "Foo", "label")]
    [InlineData("Foo.bar.baz", "Foo", "bar.baz")]
    [InlineData("SampleMod_WoodenSpear.description", "SampleMod_WoodenSpear", "description")]
    [InlineData("NoDots", "NoDots", "NoDots")]
    public void DefNameAndRealNode_SplitAtFirstDot(string node, string expectedDef, string expectedReal)
    {
        var entry = new TranslationEntry("ThingDef", node, "x", null, null, null);

        entry.DefName.Should().Be(expectedDef);
        entry.RealNode.Should().Be(expectedReal);
    }

    [Fact]
    public void Equality_IsRecordValueBased()
    {
        var a = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);
        var b = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);

        a.Should().Be(b);
    }

    [Fact]
    public void With_RecordDeconstructionSupported()
    {
        var a = new TranslationEntry("ThingDef", "Foo.label", "x", null, null, null);
        var b = a with { Translated = "y" };

        b.Translated.Should().Be("y");
        b.Original.Should().Be("x");
    }
}
