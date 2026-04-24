using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerReadMetadataTests
{
    private static readonly string[] ExpectedDependencies = ["Ludeon.RimWorld", "author.bar"];
    private static InMemoryFileSystem FsWithAbout(string modRoot, string aboutXml)
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile($"{modRoot}/About/About.xml", aboutXml);
        return fs;
    }

    [Fact]
    public void ReadMetadata_OfficialContent_MarksIsOfficial()
    {
        var fs = FsWithAbout("/rw/Data/Core", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>Core</name>
              <packageId>Ludeon.RimWorld</packageId>
              <author>Ludeon Studios</author>
            </ModMetaData>
            """);
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/rw/Data/Core");

        meta.Should().NotBeNull();
        meta!.IsOfficialContent.Should().BeTrue();
        meta.PackageId.Should().Be("Ludeon.RimWorld");
        meta.ModName.Should().Be("Core");
        meta.Id.Should().Be("Official");
    }

    [Fact]
    public void ReadMetadata_Unofficial_WithPublishedFileId()
    {
        var fs = FsWithAbout("/ws/2997308585", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>My Mod</name>
              <packageId>author.mymod</packageId>
              <author>Author</author>
            </ModMetaData>
            """);
        fs.AddFile("/ws/2997308585/About/PublishedFileId.txt", "2997308585");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/ws/2997308585");

        meta.Should().NotBeNull();
        meta!.IsOfficialContent.Should().BeFalse();
        meta.Id.Should().Be("2997308585");
    }

    [Fact]
    public void ReadMetadata_WithModDependencies_ParsesThem()
    {
        var fs = FsWithAbout("/mods/Foo", """
            <?xml version="1.0" encoding="utf-8"?>
            <ModMetaData>
              <name>Foo</name>
              <packageId>a.foo</packageId>
              <modDependencies>
                <li><packageId>Ludeon.RimWorld</packageId></li>
                <li><packageId>author.bar</packageId></li>
              </modDependencies>
            </ModMetaData>
            """);
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        var meta = lister.ReadMetadata("/mods/Foo");

        meta!.ModDependencies.Should().NotBeNull();
        meta.ModDependencies!.Should().BeEquivalentTo(ExpectedDependencies);
    }

    [Fact]
    public void ReadMetadata_MissingAbout_ReturnsNull()
    {
        var fs = new InMemoryFileSystem();
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        lister.ReadMetadata("/mods/Missing").Should().BeNull();
    }
}
