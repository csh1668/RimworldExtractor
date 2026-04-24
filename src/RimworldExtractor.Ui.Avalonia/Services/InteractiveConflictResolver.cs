using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Infrastructure.Output;

namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// <see cref="IConflictResolver"/> that shows a dialog when running in a UI context.
/// Falls back to <see cref="PolicyBasedConflictResolver"/> if no dialog service is available
/// or the application is running headless.
/// </summary>
public sealed class InteractiveConflictResolver : IConflictResolver
{
    private readonly IDialogService _dialogService;
    private readonly PolicyBasedConflictResolver _fallback;

    public InteractiveConflictResolver(IDialogService dialogService)
    {
        _dialogService = dialogService;
        _fallback = new PolicyBasedConflictResolver(DuplicatesPolicy.Overwrite);
    }

    public async Task<ConflictDecision> ResolveAsync(ConflictContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = $"File already exists:\n{context.TargetPath}\n\nOverwrite it?";
            var overwrite = await _dialogService.ShowConfirmAsync($"Conflict ({context.FileKind})", message);
            return overwrite ? ConflictDecision.Overwrite : ConflictDecision.KeepOriginal;
        }
        catch
        {
            return await _fallback.ResolveAsync(context, cancellationToken);
        }
    }
}
