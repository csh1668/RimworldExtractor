using System.Text.Json;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON with atomic writes.
/// Uses a <c>.tmp</c> sibling file and <see cref="File.Replace(string, string, string)"/>
/// so crashes never produce a partially-written file.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly string _tempPath;
    private readonly string _backupPath;

    /// <summary>
    /// Initializes a new <see cref="JsonSettingsStore"/> that reads and writes to <paramref name="path"/>.
    /// </summary>
    public JsonSettingsStore(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _tempPath = _path + ".tmp";
        _backupPath = _path + ".bak";
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return AppSettings.Default;

        await using var stream = File.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync(
            stream,
            AppSettingsJsonContext.Default.AppSettings,
            cancellationToken);
        return loaded ?? AppSettings.Default;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using (var stream = File.Create(_tempPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                AppSettingsJsonContext.Default.AppSettings,
                cancellationToken);
        }

        if (File.Exists(_path))
        {
            File.Replace(_tempPath, _path, _backupPath);
        }
        else
        {
            File.Move(_tempPath, _path);
        }
    }
}
