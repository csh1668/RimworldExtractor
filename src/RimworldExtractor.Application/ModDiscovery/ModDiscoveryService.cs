using Microsoft.Extensions.Logging;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;

namespace RimworldExtractor.Application.ModDiscovery;

/// <summary>
/// Thin wrapper over <see cref="IModLister"/> adding caching + logging for UI/CLI consumption.
/// </summary>
public sealed class ModDiscoveryService
{
    private static readonly Action<ILogger, int, Exception?> _logDiscovered =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "ModDiscovery"), "Discovered {Count} mods");

    private readonly IModLister _modLister;
    private readonly ILogger<ModDiscoveryService> _logger;
    private IReadOnlyList<ModMetadata>? _cachedAll;

    public ModDiscoveryService(IModLister modLister, ILogger<ModDiscoveryService> logger)
    {
        _modLister = modLister;
        _logger = logger;
    }

    public IReadOnlyList<ModMetadata> DiscoverAll(bool refresh = false)
    {
        if (refresh || _cachedAll is null)
        {
            _cachedAll = _modLister.DiscoverAll();
            _logDiscovered(_logger, _cachedAll.Count, null);
        }
        return _cachedAll;
    }

    public ModMetadata? ReadMetadata(string modRoot) => _modLister.ReadMetadata(modRoot);

    public IReadOnlyList<ExtractableFolder> GetExtractableFolders(ModMetadata metadata)
        => _modLister.GetExtractableFolders(metadata);

    public IReadOnlyList<ModMetadata> FindReferenceMods(ModMetadata target)
        => _modLister.FindReferenceMods(target);
}
