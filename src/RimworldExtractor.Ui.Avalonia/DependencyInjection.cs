using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Ui.Avalonia.Services;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia;

/// <summary>
/// Registers all UI-layer services and ViewModels.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<ILocalizer, NullLocalizer>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IFilePicker, AvaloniaFilePicker>();

        // Override conflict resolver with interactive one
        services.AddSingleton<IConflictResolver>(sp =>
            new InteractiveConflictResolver(sp.GetRequiredService<IDialogService>()));

        // ViewModels (transient so each navigation creates a fresh instance)
        services.AddTransient<ModSelectViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ExtractViewModel>();
        services.AddTransient<MainViewModel>();

        return services;
    }
}
