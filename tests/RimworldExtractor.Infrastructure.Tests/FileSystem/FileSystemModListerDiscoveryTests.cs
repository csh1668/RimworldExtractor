using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerDiscoveryTests
{
    [Fact]
    public void DiscoverAll_EnumeratesOfficialLocalAndWorkshop()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/rw/Data/Core/About/About.xml", "<ModMetaData><packageId>Ludeon.RimWorld</packageId><name>Core</name><author>Ludeon Studios</author></ModMetaData>");
        fs.AddFile("/rw/Mods/Local/About/About.xml", "<ModMetaData><packageId>local.mod</packageId><name>Local</name></ModMetaData>");
        fs.AddFile("/ws/123/About/About.xml", "<ModMetaData><packageId>ws.mod</packageId><name>WS</name></ModMetaData>");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");

        var all = lister.DiscoverAll();

        all.Should().HaveCount(3);
        all.Should().Contain(m => m.PackageId == "Ludeon.RimWorld" && m.IsOfficialContent);
        all.Should().Contain(m => m.PackageId == "local.mod");
        all.Should().Contain(m => m.PackageId == "ws.mod");
    }

    [Fact]
    public void FindReferenceMods_ReturnsTransitiveDependencies()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/rw/Data/Core/About/About.xml", "<ModMetaData><packageId>Ludeon.RimWorld</packageId><name>Core</name><author>Ludeon Studios</author></ModMetaData>");
        fs.AddFile("/rw/Mods/A/About/About.xml", "<ModMetaData><packageId>a.mod</packageId><name>A</name><modDependencies><li><packageId>b.mod</packageId></li></modDependencies></ModMetaData>");
        fs.AddFile("/rw/Mods/B/About/About.xml", "<ModMetaData><packageId>b.mod</packageId><name>B</name></ModMetaData>");
        var lister = new FileSystemModLister(fs, currentVersion: "1.6",
            originalLanguageFolderName: "English",
            rimworldDir: "/rw", workshopDir: "/ws");
        var modA = lister.DiscoverAll().First(m => m.PackageId == "a.mod");

        var refs = lister.FindReferenceMods(modA);

        refs.Select(m => m.PackageId).Should().Contain("Ludeon.RimWorld");
        refs.Select(m => m.PackageId).Should().Contain("b.mod");
    }
}
