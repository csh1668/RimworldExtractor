namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Higher-level file-system helpers: BFS enumeration, directory creation with combine.
/// Delegates to an <see cref="IFileSystem"/> so it works with any FS backend.
/// </summary>
public sealed class FileSystemGateway
{
    private readonly IFileSystem _fs;

    public FileSystemGateway(IFileSystem fs) => _fs = fs;

    /// <summary>BFS-enumerates all files under <paramref name="root"/>; subdirectories and files are each sorted ordinally for determinism.</summary>
    public IEnumerable<string> DescendantFiles(string root)
    {
        if (!_fs.DirectoryExists(root)) yield break;

        var queue = new Queue<string>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var sub in _fs.EnumerateDirectories(current).OrderBy(x => x, StringComparer.Ordinal))
                queue.Enqueue(sub);
            foreach (var file in _fs.EnumerateFiles(current).OrderBy(x => x, StringComparer.Ordinal))
                yield return file;
        }
    }

    /// <summary>Combines the given path segments, creates the directory if missing, returns the full path.</summary>
    public string PathCombineCreateDir(params string[] parts)
    {
        var combined = Path.Combine(parts);
        if (!_fs.DirectoryExists(combined)) _fs.CreateDirectory(combined);
        return combined;
    }
}
