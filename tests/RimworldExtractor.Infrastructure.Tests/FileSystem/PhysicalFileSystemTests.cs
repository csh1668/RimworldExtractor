using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class PhysicalFileSystemTests : IDisposable
{
    private static readonly string[] ExpectedTopLevelFiles = ["a.txt", "b.txt"];

    private readonly string _tmpDir;
#pragma warning disable CA1859 // Interface field intentional — tests the IFileSystem contract
    private readonly IFileSystem _fs;
#pragma warning restore CA1859

    public PhysicalFileSystemTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-pfs-").FullName;
        _fs = new PhysicalFileSystem();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* best-effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task WriteAndReadAllText_RoundTrips()
    {
        var path = Path.Combine(_tmpDir, "a.txt");

        await _fs.WriteAllTextAsync(path, "hello", TestContext.Current.CancellationToken);
        var read = await _fs.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        read.Should().Be("hello");
    }

    [Fact]
    public void FileExists_False_ForMissingFile()
    {
        _fs.FileExists(Path.Combine(_tmpDir, "missing.txt")).Should().BeFalse();
    }

    [Fact]
    public void CreateDirectory_IsIdempotent()
    {
        var path = Path.Combine(_tmpDir, "nested", "deep");

        _fs.CreateDirectory(path);
        _fs.CreateDirectory(path);

        _fs.DirectoryExists(path).Should().BeTrue();
    }

    [Fact]
    public void EnumerateFiles_ReturnsOnlyTopLevelFiles()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_tmpDir, "b.txt"), "b");
        Directory.CreateDirectory(Path.Combine(_tmpDir, "sub"));
        File.WriteAllText(Path.Combine(_tmpDir, "sub", "c.txt"), "c");

        var files = _fs.EnumerateFiles(_tmpDir).Select(Path.GetFileName).ToHashSet();

        files.Should().BeEquivalentTo(ExpectedTopLevelFiles);
    }
}
