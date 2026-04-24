using FluentAssertions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Tests.Entities;

public class ModMetadataTests
{
    [Fact]
    public void Ctor_StoresAllFields()
    {
        var meta = new ModMetadata(
            RootDir: "/Mods/Foo",
            Id: "2997308585",
            ModName: "Foo Mod",
            PackageId: "author.foo",
            IsOfficialContent: false,
            ModDependencies: new List<string> { "Ludeon.RimWorld" });

        meta.RootDir.Should().Be("/Mods/Foo");
        meta.Id.Should().Be("2997308585");
        meta.ModName.Should().Be("Foo Mod");
        meta.PackageId.Should().Be("author.foo");
        meta.IsOfficialContent.Should().BeFalse();
        meta.ModDependencies.Should().ContainSingle().Which.Should().Be("Ludeon.RimWorld");
    }

    [Fact]
    public void Identifier_Official_IsJustModName()
    {
        var meta = new ModMetadata("/Data/Core", "Core", "Core", "Ludeon.RimWorld", IsOfficialContent: true);

        meta.Identifier.Should().Be("Core");
    }

    [Fact]
    public void Identifier_Unofficial_WithKnownId_IsModNameDashId()
    {
        var meta = new ModMetadata("/Mods/Foo", "2997308585", "Foo Mod", "author.foo", IsOfficialContent: false);

        meta.Identifier.Should().Be("Foo Mod - 2997308585");
    }

    [Fact]
    public void Identifier_Unofficial_WithPlaceholderId_IsJustModName()
    {
        var meta = new ModMetadata("/Mods/Foo", "???", "Foo Mod", "author.foo", IsOfficialContent: false);

        meta.Identifier.Should().Be("Foo Mod");
    }

    [Fact]
    public void Empty_IsDefaultSingleton()
    {
        var a = ModMetadata.Empty;
        var b = ModMetadata.Empty;

        a.Should().BeSameAs(b);
        a.RootDir.Should().BeEmpty();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var a = new ModMetadata("/a", "1", "m", "a.m", true);
        var b = new ModMetadata("/a", "1", "m", "a.m", true);

        a.Should().Be(b);
    }
}
