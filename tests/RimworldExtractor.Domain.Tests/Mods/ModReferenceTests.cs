using FluentAssertions;
using RimworldExtractor.Domain.Mods;

namespace RimworldExtractor.Domain.Tests.Mods;

public class ModReferenceTests
{
    [Fact]
    public void ByPackageId_StoresKindAndValue()
    {
        var r = ModReference.ByPackageId("Ludeon.RimWorld");

        r.Kind.Should().Be(ModReferenceKind.PackageId);
        r.Value.Should().Be("Ludeon.RimWorld");
    }

    [Fact]
    public void ByModName_StoresKindAndValue()
    {
        var r = ModReference.ByModName("Core");

        r.Kind.Should().Be(ModReferenceKind.ModName);
        r.Value.Should().Be("Core");
    }

    [Theory]
    [InlineData(ModReferenceKind.PackageId)]
    [InlineData(ModReferenceKind.ModName)]
    public void Construct_WithEmptyValue_Throws(ModReferenceKind kind)
    {
        var act = () => new ModReference("", kind);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsValueBased_AndKindSensitive()
    {
        var a = ModReference.ByPackageId("Foo");
        var b = ModReference.ByPackageId("Foo");
        var c = ModReference.ByModName("Foo");

        a.Should().Be(b);
        a.Should().NotBe(c, "different Kind means different reference even with the same Value");
    }
}
