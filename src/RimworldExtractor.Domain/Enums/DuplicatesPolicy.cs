namespace RimworldExtractor.Domain.Enums;

/// <summary>
/// How to handle a translation entry whose key collides with an existing one.
/// Ordinals preserved from legacy Prefabs.dat schema for migration compatibility.
/// </summary>
public enum DuplicatesPolicy
{
    Stop = 0,
    Overwrite = 1,
    KeepOriginal = 2,
}
