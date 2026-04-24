using FluentAssertions;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Tests.FileSystem;

public class SafeFileWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenTargetAbsent_MovesTempToTarget()
    {
        var fs = new InMemoryFileSystem();
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/out.txt", stream =>
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
            stream.Write(bytes);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        var text = await fs.ReadAllTextAsync("/out.txt", TestContext.Current.CancellationToken);
        text.Should().Be("hello");
        fs.FileExists("/out.txt.tmp").Should().BeFalse();
    }

    [Fact]
    public async Task WriteAsync_WhenTargetExists_CreatesBackup()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/out.txt", "old");
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/out.txt", stream =>
        {
            stream.Write(System.Text.Encoding.UTF8.GetBytes("new"));
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        var text = await fs.ReadAllTextAsync("/out.txt", TestContext.Current.CancellationToken);
        var backup = await fs.ReadAllTextAsync("/out.txt.bak", TestContext.Current.CancellationToken);
        text.Should().Be("new");
        backup.Should().Be("old");
    }

    [Fact]
    public async Task WriteAsync_CreatesParentDirectoryIfMissing()
    {
        var fs = new InMemoryFileSystem();
        var writer = new SafeFileWriter(fs);

        await writer.WriteAsync("/nested/deep/out.txt", stream =>
        {
            stream.Write(System.Text.Encoding.UTF8.GetBytes("x"));
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        fs.DirectoryExists("/nested/deep").Should().BeTrue();
    }
}
