namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Writes a file atomically: content is streamed to <c>path.tmp</c>, then <see cref="IFileSystem.Replace"/>
/// (or <see cref="IFileSystem.Move"/> when the target is absent) promotes it. A crash mid-write leaves
/// only the orphan temp file; the target is never partially written.
/// </summary>
public sealed class SafeFileWriter
{
    private readonly IFileSystem _fs;

    public SafeFileWriter(IFileSystem fs) => _fs = fs;

    /// <param name="targetPath">Final destination path.</param>
    /// <param name="write">Callback that writes payload bytes to the provided stream.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    public async Task WriteAsync(string targetPath, Func<Stream, Task> write, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !_fs.DirectoryExists(directory))
            _fs.CreateDirectory(directory);

        var tempPath = targetPath + ".tmp";
        var backupPath = targetPath + ".bak";

        await using (var stream = _fs.CreateFile(tempPath))
        {
            await write(stream);
        }

        if (_fs.FileExists(targetPath))
            _fs.Replace(tempPath, targetPath, backupPath);
        else
            _fs.Move(tempPath, targetPath);

        cancellationToken.ThrowIfCancellationRequested();
    }
}
