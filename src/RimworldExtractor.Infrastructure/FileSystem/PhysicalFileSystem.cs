namespace RimworldExtractor.Infrastructure.FileSystem;

/// <summary>
/// Default <see cref="IFileSystem"/> implementation backed by the real filesystem.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, contents, cancellationToken);
    public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllLinesAsync(path, cancellationToken);
    public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        => File.WriteAllLinesAsync(path, lines, cancellationToken);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Stream OpenWrite(string path) => File.OpenWrite(path);
    public Stream CreateFile(string path) => File.Create(path);
    public void Move(string source, string destination) => File.Move(source, destination);
    public void Replace(string source, string destination, string? backup) => File.Replace(source, destination, backup);
    public void Delete(string path) => File.Delete(path);
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);
}
