using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemModListerExtractableFoldersTests
{
    private static FileSystemModLister Lister(InMemoryFileSystem fs) =>
        new(fs, currentVersion: "1.6", originalLanguageFolderName: "English", rimworldDir: "/rw", workshopDir: "/ws");

    [Fact]
    public void GetExtractableFolders_WithoutLoadFolders_FindsTopLevelDirs()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/Defs/ThingDefs.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/Patches/p.xml", "<Patch/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        var folderNames = folders.Select(f => f.FolderName).ToHashSet();
        folderNames.Should().Contain("Defs");
        folderNames.Should().Contain("Patches");
    }

    [Fact]
    public void GetExtractableFolders_WithVersionSubdir_AddsVersionedFolders()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/1.6/Defs/ThingDefs.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/Common/Defs/Shared.xml", "<Defs/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        folders.Should().Contain(f => f.FolderName.Contains("1.6") && f.FolderName.Contains("Defs") && f.VersionInfo == "1.6");
        folders.Should().Contain(f => f.FolderName.Contains("Common") && f.FolderName.Contains("Defs") && f.VersionInfo == "Common");
    }

    [Fact]
    public void GetExtractableFolders_WithLoadFolders_RespectsLoadFoldersXml()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/mods/Foo/About/About.xml", "<ModMetaData><packageId>a.b</packageId><name>F</name></ModMetaData>");
        fs.AddFile("/mods/Foo/LoadFolders.xml", """
            <loadFolders>
              <v1.6>
                <li>extra</li>
                <li IfModActive="dep.x">conditional</li>
              </v1.6>
            </loadFolders>
            """);
        fs.AddFile("/mods/Foo/extra/Defs/Things.xml", "<Defs/>");
        fs.AddFile("/mods/Foo/conditional/Defs/Cond.xml", "<Defs/>");
        var lister = Lister(fs);
        var mod = lister.ReadMetadata("/mods/Foo")!;

        var folders = lister.GetExtractableFolders(mod);

        folders.Should().Contain(f => f.FolderName.Contains("extra") && f.VersionInfo == "1.6");
        folders.Should().Contain(f => f.FolderName.Contains("conditional") && f.RequiredPackageId == "dep.x");
    }
}
