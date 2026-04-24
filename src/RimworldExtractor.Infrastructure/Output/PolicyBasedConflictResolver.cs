using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;

namespace RimworldExtractor.Infrastructure.Output;

/// <summary>
/// Non-interactive <see cref="IConflictResolver"/> that always returns the fixed decision
/// implied by a <see cref="DuplicatesPolicy"/>. Suitable for CLI and tests.
/// </summary>
public sealed class PolicyBasedConflictResolver : IConflictResolver
{
    private readonly DuplicatesPolicy _policy;

    public PolicyBasedConflictResolver(DuplicatesPolicy policy) => _policy = policy;

    public Task<ConflictDecision> ResolveAsync(ConflictContext context, CancellationToken cancellationToken = default)
    {
        var decision = _policy switch
        {
            DuplicatesPolicy.Overwrite => ConflictDecision.Overwrite,
            DuplicatesPolicy.KeepOriginal => ConflictDecision.KeepOriginal,
            DuplicatesPolicy.Stop => ConflictDecision.Abort,
            _ => throw new InvalidOperationException($"Unknown DuplicatesPolicy value: {_policy}"),
        };
        return Task.FromResult(decision);
    }
}
