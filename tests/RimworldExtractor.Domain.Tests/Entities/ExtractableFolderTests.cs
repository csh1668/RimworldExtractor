using FluentAssertions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Domain.Tests.Entities;

public class ExtractableFolderTests
{
    [Fact]
    public void Ctor_WithDefaults_SetsDefaultVersion()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "Defs", RequiredPackageId: null);

        folder.Root.Should().Be(mod);
        folder.FolderName.Should().Be("Defs");
        folder.RequiredPackageId.Should().BeNull();
        folder.VersionInfo.Should().Be("default");
    }

    [Fact]
    public void FullPath_CombinesRootDirAndFolderName()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "1.6/Defs", null);

        folder.FullPath.Should().Be(Path.Combine("/Mods/Foo", "1.6/Defs"));
    }

    [Fact]
    public void VersionInfo_CanBeExplicit()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var folder = new ExtractableFolder(mod, "1.6/Defs", null, VersionInfo: "1.6");

        folder.VersionInfo.Should().Be("1.6");
    }

    [Fact]
    public void Equality_IsStructural()
    {
        var mod = new ModMetadata("/Mods/Foo", "1", "Foo", "author.foo", false);
        var a = new ExtractableFolder(mod, "Defs", null);
        var b = new ExtractableFolder(mod, "Defs", null);

        a.Should().Be(b);
    }
}
