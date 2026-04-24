using System.Text;

namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// In-memory <see cref="IFileSystem"/> for deterministic testing. Thread-unsafe — one
/// instance per test. Paths are case-sensitive and use forward slashes.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private static readonly string[] LineSeparators = ["\r\n", "\n"];
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);

    public InMemoryFileSystem()
    {
        _directories.Add("/");
    }

    public void AddFile(string path, string contents)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(contents);
    }

    public bool FileExists(string path) => _files.ContainsKey(path);
    public bool DirectoryExists(string path) => _directories.Contains(path);

    public void CreateDirectory(string path)
    {
        var parts = path.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        foreach (var part in parts)
        {
            current = current + "/" + part;
            _directories.Add(current);
        }
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        return Task.FromResult(Encoding.UTF8.GetString(bytes));
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(contents);
        return Task.CompletedTask;
    }

    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        var text = Encoding.UTF8.GetString(bytes);
        return Task.FromResult(text.Split(LineSeparators, StringSplitOptions.None));
    }

    public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        EnsureParentDirs(path);
        _files[path] = Encoding.UTF8.GetBytes(string.Join('\n', lines) + '\n');
        return Task.CompletedTask;
    }

    public Stream OpenRead(string path)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException("File not found in InMemoryFileSystem", path);
        return new MemoryStream(bytes, writable: false);
    }

    public Stream OpenWrite(string path) => CreateFile(path);

    public Stream CreateFile(string path)
    {
        EnsureParentDirs(path);
        var ms = new WritebackStream(bytes => _files[path] = bytes);
        return ms;
    }

    public void Move(string source, string destination)
    {
        if (!_files.TryGetValue(source, out var bytes))
            throw new FileNotFoundException("Source not found in InMemoryFileSystem", source);
        EnsureParentDirs(destination);
        _files[destination] = bytes;
        _files.Remove(source);
    }

    public void Replace(string source, string destination, string? backup)
    {
        if (!_files.TryGetValue(source, out var newBytes))
            throw new FileNotFoundException("Source not found in InMemoryFileSystem", source);
        if (_files.TryGetValue(destination, out var oldBytes) && backup is not null)
        {
            EnsureParentDirs(backup);
            _files[backup] = oldBytes;
        }
        _files[destination] = newBytes;
        _files.Remove(source);
    }

    public void Delete(string path) => _files.Remove(path);

    public IEnumerable<string> EnumerateFiles(string path)
    {
        var prefix = path.TrimEnd('/') + "/";
        return _files.Keys
            .Where(p => p.StartsWith(prefix, StringComparison.Ordinal))
            .Where(p => !p[prefix.Length..].Contains('/'))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        var prefix = path.TrimEnd('/') + "/";
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dir in _directories)
        {
            if (!dir.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var suffix = dir[prefix.Length..];
            if (suffix.Length == 0 || suffix.Contains('/')) continue;
            result.Add(dir);
        }
        return result.OrderBy(p => p, StringComparer.Ordinal);
    }

    private void EnsureParentDirs(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        if (lastSlash > 0) CreateDirectory(path[..lastSlash]);
    }

    private sealed class WritebackStream : MemoryStream
    {
        private readonly Action<byte[]> _onDispose;
        private bool _disposed;
        public WritebackStream(Action<byte[]> onDispose) { _onDispose = onDispose; }
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _onDispose(ToArray());
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
