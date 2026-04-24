using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class InMemoryFileSystemTests
{
    [Fact]
    public async Task WriteThenRead_RoundTrips()
    {
        var fs = new InMemoryFileSystem();

        await fs.WriteAllTextAsync("/foo.txt", "hello", TestContext.Current.CancellationToken);
        var read = await fs.ReadAllTextAsync("/foo.txt", TestContext.Current.CancellationToken);

        read.Should().Be("hello");
    }

    [Fact]
    public async Task ReadNonexistent_Throws()
    {
        var fs = new InMemoryFileSystem();

        var act = () => fs.ReadAllTextAsync("/missing.txt", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void FileExists_ReflectsAddFile()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/foo.txt", "x");

        fs.FileExists("/foo.txt").Should().BeTrue();
        fs.FileExists("/bar.txt").Should().BeFalse();
    }

    [Fact]
    public void EnumerateFiles_ReturnsOnlyImmediateChildren()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/a/1.txt", "");
        fs.AddFile("/a/2.txt", "");
        fs.AddFile("/a/b/3.txt", "");

        var files = fs.EnumerateFiles("/a").Select(p => p).ToList();

        files.Should().HaveCount(2);
        files.Should().Contain("/a/1.txt");
        files.Should().Contain("/a/2.txt");
    }

    [Fact]
    public void CreateDirectory_IsIdempotent()
    {
        var fs = new InMemoryFileSystem();
        fs.CreateDirectory("/a/b/c");
        fs.CreateDirectory("/a/b/c");

        fs.DirectoryExists("/a/b/c").Should().BeTrue();
        fs.DirectoryExists("/a/b").Should().BeTrue();
        fs.DirectoryExists("/a").Should().BeTrue();
    }

    [Fact]
    public async Task Replace_SwapsContents_AndCreatesBackup()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/target.txt", "old");
        fs.AddFile("/new.txt", "new");

        fs.Replace("/new.txt", "/target.txt", "/target.txt.bak");

        (await fs.ReadAllTextAsync("/target.txt", TestContext.Current.CancellationToken)).Should().Be("new");
        (await fs.ReadAllTextAsync("/target.txt.bak", TestContext.Current.CancellationToken)).Should().Be("old");
        fs.FileExists("/new.txt").Should().BeFalse();
    }
}
