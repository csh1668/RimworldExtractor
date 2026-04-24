using RimworldExtractor.Domain.Settings;

namespace RimworldExtractor.Domain.Abstractions;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> to a durable location. Implementations
/// must provide atomic writes (i.e. partial failure never produces a corrupt file).
/// </summary>
public interface ISettingsStore
{
    /// <summary>Returns persisted settings, or <see cref="AppSettings.Default"/> if no store exists yet.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the given settings atomically. Existing file is replaced or backed up.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
