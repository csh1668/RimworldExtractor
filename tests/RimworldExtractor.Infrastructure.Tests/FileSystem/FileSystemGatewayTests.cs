using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class FileSystemGatewayTests
{
    [Fact]
    public void DescendantFiles_YieldsBfsOrderedFiles()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/z.xml", "");
        fs.AddFile("/root/a.xml", "");
        fs.AddFile("/root/sub2/y.xml", "");
        fs.AddFile("/root/sub1/b.xml", "");
        var gateway = new FileSystemGateway(fs);

        var result = gateway.DescendantFiles("/root").ToList();

        // BFS: all files of /root first (sorted), then /sub1, then /sub2.
        result.Should().Equal(
            "/root/a.xml",
            "/root/z.xml",
            "/root/sub1/b.xml",
            "/root/sub2/y.xml");
    }

    [Fact]
    public void DescendantFiles_ReturnsEmpty_ForMissingRoot()
    {
        var fs = new InMemoryFileSystem();
        var gateway = new FileSystemGateway(fs);

        gateway.DescendantFiles("/missing").Should().BeEmpty();
    }

    [Fact]
    public void PathCombineCreateDir_CreatesAllIntermediateDirs()
    {
        var fs = new InMemoryFileSystem();
        var gateway = new FileSystemGateway(fs);

        var path = gateway.PathCombineCreateDir("/root", "a", "b");

        path.Should().Be(Path.Combine("/root", "a", "b"));
        fs.DirectoryExists(path.Replace('\\', '/')).Should().BeTrue();
    }
}
