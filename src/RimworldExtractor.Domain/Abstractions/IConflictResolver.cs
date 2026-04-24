namespace RimworldExtractor.Domain.Abstractions;

/// <summary>What to do when a target file already exists.</summary>
public enum ConflictDecision
{
    /// <summary>Overwrite the existing file with the new content.</summary>
    Overwrite = 0,
    /// <summary>Keep the existing file, drop the new content.</summary>
    KeepOriginal = 1,
    /// <summary>Abort the entire write operation.</summary>
    Abort = 2,
}

/// <summary>Information about a pending file-write conflict.</summary>
/// <param name="TargetPath">Absolute path of the conflicting file.</param>
/// <param name="FileKind">Human-readable file type (e.g. "XLSX", "XML", "TXT").</param>
public sealed record ConflictContext(string TargetPath, string FileKind);

/// <summary>
/// Decides what to do when a file-write would overwrite an existing file. Implementations:
/// <c>PolicyBasedConflictResolver</c> (non-interactive),
/// <c>InteractiveConflictResolver</c> (prompts the user).
/// </summary>
public interface IConflictResolver
{
    Task<ConflictDecision> ResolveAsync(ConflictContext context, CancellationToken cancellationToken = default);
}
