using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application;
using RimworldExtractor.Infrastructure;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia;

public partial class App : global::Avalonia.Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Determine settings path — next to the executable
        var settingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "settings.json");

        services
            .AddLogging()
            .AddInfrastructure(settingsPath)
            .AddApplication()
            .AddUi();

        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = _services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
