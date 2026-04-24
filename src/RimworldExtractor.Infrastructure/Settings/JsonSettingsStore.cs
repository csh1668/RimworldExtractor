using System.Text.Json;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.FileSystem;

namespace RimworldExtractor.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON with atomic writes via <see cref="SafeFileWriter"/>.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IFileSystem _fs;
    private readonly SafeFileWriter _safeWriter;
    private readonly string _path;

    /// <summary>
    /// Initializes a new <see cref="JsonSettingsStore"/> that reads and writes to <paramref name="path"/>.
    /// </summary>
    public JsonSettingsStore(IFileSystem fs, string path)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _safeWriter = new SafeFileWriter(fs);
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_fs.FileExists(_path)) return AppSettings.Default;

        await using var stream = _fs.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync(
            stream,
            AppSettingsJsonContext.Default.AppSettings,
            cancellationToken);
        return loaded ?? AppSettings.Default;
    }

    /// <inheritdoc />
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        return _safeWriter.WriteAsync(_path, async stream =>
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                AppSettingsJsonContext.Default.AppSettings,
                cancellationToken);
        }, cancellationToken);
    }
}
