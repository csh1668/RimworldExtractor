using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Ui.Avalonia.Services;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private static MainViewModel CreateMainVm()
    {
        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(Array.Empty<ModMetadata>());
        lister.GetExtractableFolders(Arg.Any<ModMetadata>()).Returns(Array.Empty<ExtractableFolder>());
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        var picker = Substitute.For<IFilePicker>();
        var store = Substitute.For<ISettingsStore>();
        store.LoadAsync(default).ReturnsForAnyArgs(AppSettings.Default);
        var pipeline = Substitute.For<IExtractionPipeline>();

        var modSelectVm = new ModSelectViewModel(service, dialog);
        var extractVm = new ExtractViewModel(pipeline, store, dialog, picker, modSelectVm);
        var settingsVm = new SettingsViewModel(store, picker, dialog);

        return new MainViewModel(extractVm, settingsVm);
    }

    [Fact]
    public void Constructor_ShouldExposeExtractAndSettings()
    {
        var vm = CreateMainVm();

        vm.Extract.Should().NotBeNull();
        vm.Settings.Should().NotBeNull();
    }

    [Fact]
    public void SelectedTabIndex_ShouldDefaultToZero()
    {
        var vm = CreateMainVm();

        vm.SelectedTabIndex.Should().Be(0);
    }

    [Fact]
    public void SelectedTabIndex_ShouldBeSettable()
    {
        var vm = CreateMainVm();

        vm.SelectedTabIndex = 1;

        vm.SelectedTabIndex.Should().Be(1);
    }

    [Fact]
    public void Extract_ShouldHaveModSelectViewModelNested()
    {
        var vm = CreateMainVm();

        vm.Extract.ModSelect.Should().NotBeNull();
    }
}
