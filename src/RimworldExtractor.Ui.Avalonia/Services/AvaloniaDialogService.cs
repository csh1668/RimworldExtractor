using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace RimworldExtractor.Ui.Avalonia.Services;

/// <summary>
/// Avalonia 11 implementation of <see cref="IDialogService"/> using built-in message box dialogs.
/// Falls back to a no-op if there is no main window (e.g. headless test runner).
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private static Window? GetMainWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window is null)
            return;

        var tcs = new TaskCompletionSource();

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var okButton = new Button
        {
            Content = "OK",
            Margin = new Thickness(8, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        okButton.Click += (_, _) =>
        {
            dialog.Close();
            tcs.TrySetResult();
        };

        dialog.Content = BuildPanel(message, okButton);
        dialog.Closed += (_, _) => tcs.TrySetResult();

        await dialog.ShowDialog(window);
        await tcs.Task;
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window is null)
            return false;

        var result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var yesButton = new Button { Content = "Yes", Margin = new Thickness(8, 0) };
        var noButton = new Button { Content = "No", Margin = new Thickness(8, 0) };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        buttonRow.Children.Add(yesButton);
        buttonRow.Children.Add(noButton);

        yesButton.Click += (_, _) => { result = true; dialog.Close(); };
        noButton.Click += (_, _) => { result = false; dialog.Close(); };

        dialog.Content = BuildPanel(message, buttonRow);

        await dialog.ShowDialog(window);
        return result;
    }

    private static StackPanel BuildPanel(string message, Control buttons)
    {
        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16, 16, 16, 8),
        };

        var container = new StackPanel();
        container.Children.Add(textBlock);
        container.Children.Add(buttons);
        return container;
    }
}
