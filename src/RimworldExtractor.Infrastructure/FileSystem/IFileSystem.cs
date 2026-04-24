namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// File-system abstraction. Infrastructure code should depend on this, never the static
/// <see cref="File"/> / <see cref="Directory"/> classes, so that a test double can be
/// substituted. The concrete <c>PhysicalFileSystem</c> wraps real disk IO.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    Stream CreateFile(string path);
    void Move(string source, string destination);
    void Replace(string source, string destination, string? backup);
    void Delete(string path);
    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateDirectories(string path);
}
