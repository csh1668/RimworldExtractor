using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Enums;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Domain.ValueObjects;
using RimworldExtractor.Ui.Avalonia.Services;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    private static AppSettings MakeSettings(
        string rimworld = "C:/RimWorld",
        string workshop = "C:/Workshop",
        string baseRefList = "",
        string original = "English",
        string translation = "Korean (한국어)")
    {
        return new AppSettings(
            SchemaVersion: AppSettings.CurrentSchemaVersion,
            Paths: new PathSettings(rimworld, workshop, baseRefList),
            Languages: new LanguageSettings(
                LanguageCode.Create(original),
                LanguageCode.Create(translation)),
            Extraction: ExtractionSettings.Default,
            Output: OutputSettings.Default);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();

        var vm = new SettingsViewModel(store, picker, dialog);

        vm.RimworldPath.Should().Be(string.Empty);
        vm.WorkshopPath.Should().Be(string.Empty);
        vm.GameVersion.Should().Be("1.6");
        vm.OriginalLanguage.Should().Be("English");
        vm.TranslationLanguage.Should().Be("Korean (한국어)");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePropertiesFromStore()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        var settings = MakeSettings(rimworld: "C:/Games/RimWorld", workshop: "C:/Steam/Workshop");
        store.LoadAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(settings);

        var vm = new SettingsViewModel(store, picker, dialog);
        await vm.LoadAsync();

        vm.RimworldPath.Should().Be("C:/Games/RimWorld");
        vm.WorkshopPath.Should().Be("C:/Steam/Workshop");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        var vm = new SettingsViewModel(store, picker, dialog);

        vm.RimworldPath = "C:/NewPath";
        vm.WorkshopPath = "C:/NewWorkshop";
        vm.CommentOriginal = true;
        vm.EnableTkey = true;
        vm.DuplicatesPolicy = (int)DuplicatesPolicy.KeepOriginal;
        vm.ExtractionFormat = (int)ExtractionFormat.Excel;

        vm.RimworldPath.Should().Be("C:/NewPath");
        vm.WorkshopPath.Should().Be("C:/NewWorkshop");
        vm.CommentOriginal.Should().BeTrue();
        vm.EnableTkey.Should().BeTrue();
        vm.DuplicatesPolicy.Should().Be((int)DuplicatesPolicy.KeepOriginal);
        vm.ExtractionFormat.Should().Be((int)ExtractionFormat.Excel);
    }

    [Fact]
    public async Task SaveCommand_ShouldCallSettingsStoreSaveAsync()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        var settings = MakeSettings();
        store.LoadAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(settings);
        dialog.ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var vm = new SettingsViewModel(store, picker, dialog);
        vm.RimworldPath = "C:/Updated";

        await vm.SaveCommand.ExecuteAsync(null);

        await store.ReceivedWithAnyArgs(1).SaveAsync(Arg.Any<AppSettings>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SaveCommand_WhenStoreFails_ShouldShowErrorDialog()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        store.LoadAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(MakeSettings());
        store.SaveAsync(Arg.Any<AppSettings>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("disk error"));
        dialog.ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var vm = new SettingsViewModel(store, picker, dialog);

        await vm.SaveCommand.ExecuteAsync(null);

        await dialog.ReceivedWithAnyArgs(1).ShowMessageAsync("Error", Arg.Any<string>());
    }

    [Fact]
    public async Task BrowseRimworldPathCommand_WhenPathSelected_ShouldUpdateRimworldPath()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        picker.PickFolderAsync(Arg.Any<string>()).Returns("C:/PickedRimWorld");

        var vm = new SettingsViewModel(store, picker, dialog);
        await vm.BrowseRimworldPathCommand.ExecuteAsync(null);

        vm.RimworldPath.Should().Be("C:/PickedRimWorld");
    }

    [Fact]
    public async Task BrowseRimworldPathCommand_WhenCancelled_ShouldNotChangeRimworldPath()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        picker.PickFolderAsync(Arg.Any<string>()).Returns((string?)null);

        var vm = new SettingsViewModel(store, picker, dialog);
        vm.RimworldPath = "C:/Original";
        await vm.BrowseRimworldPathCommand.ExecuteAsync(null);

        vm.RimworldPath.Should().Be("C:/Original");
    }

    [Fact]
    public async Task IsSaving_ShouldReturnToFalse_AfterSaveCompletes()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        var settings = MakeSettings();
        store.LoadAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(settings);
        dialog.ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var vm = new SettingsViewModel(store, picker, dialog);

        vm.IsSaving.Should().BeFalse();
        await vm.SaveCommand.ExecuteAsync(null);
        vm.IsSaving.Should().BeFalse();
    }

    [Fact]
    public void GameVersion_ShouldDefaultTo16()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();

        var vm = new SettingsViewModel(store, picker, dialog);

        vm.GameVersion.Should().Be("1.6");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetCommentOriginalFromStore()
    {
        var store = Substitute.For<ISettingsStore>();
        var picker = Substitute.For<IFilePicker>();
        var dialog = Substitute.For<IDialogService>();
        var settings = MakeSettings() with
        {
            Extraction = ExtractionSettings.Default with { CommentOriginal = true },
        };
        store.LoadAsync(TestContext.Current.CancellationToken).ReturnsForAnyArgs(settings);

        var vm = new SettingsViewModel(store, picker, dialog);
        await vm.LoadAsync();

        vm.CommentOriginal.Should().BeTrue();
    }
}
