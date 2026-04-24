using Avalonia.Controls;
using Avalonia.Controls.Templates;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var viewModelTypeName = data.GetType().FullName!;
        var viewTypeName = viewModelTypeName
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var type = Type.GetType(viewTypeName);
        if (type is null)
            return new TextBlock { Text = $"View not found: {viewTypeName}" };

        return (Control)Activator.CreateInstance(type)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
