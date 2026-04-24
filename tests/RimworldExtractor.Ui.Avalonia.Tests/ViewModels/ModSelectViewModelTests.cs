using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Domain.Entities;
using RimworldExtractor.Ui.Avalonia.Services;
using RimworldExtractor.Ui.Avalonia.ViewModels;

namespace RimworldExtractor.Ui.Avalonia.Tests.ViewModels;

public sealed class ModSelectViewModelTests
{
    private static ModMetadata MakeMod(string name = "TestMod", string id = "123")
        => new ModMetadata(
            RootDir: $"C:/mods/{id}",
            Id: id,
            ModName: name,
            PackageId: $"author.{name.ToLowerInvariant()}",
            IsOfficialContent: false);

    private static (ModSelectViewModel vm, IModLister lister) CreateVm(
        IReadOnlyList<ModMetadata>? mods = null)
    {
        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(mods ?? Array.Empty<ModMetadata>());
        lister.GetExtractableFolders(Arg.Any<ModMetadata>()).Returns(Array.Empty<ExtractableFolder>());
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();

        return (new ModSelectViewModel(service, dialog), lister);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyState()
    {
        var (vm, _) = CreateVm();

        vm.Mods.Should().BeEmpty();
        vm.SelectedMod.Should().BeNull();
        vm.SearchText.Should().Be(string.Empty);
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadModsCommand_ShouldPopulateModsList()
    {
        var mods = new[] { MakeMod("Alpha", "1"), MakeMod("Beta", "2") };
        var (vm, _) = CreateVm(mods);

        await vm.LoadModsCommand.ExecuteAsync(null);

        vm.Mods.Should().HaveCount(2);
        vm.Mods[0].DisplayName.Should().Contain("Alpha");
        vm.Mods[1].DisplayName.Should().Contain("Beta");
    }

    [Fact]
    public async Task LoadModsCommand_WithSearchFilter_ShouldFilterMods()
    {
        var mods = new[] { MakeMod("AlphaMod", "1"), MakeMod("BetaMod", "2"), MakeMod("AlphaTool", "3") };
        var (vm, _) = CreateVm(mods);

        vm.SearchText = "Alpha";
        await vm.LoadModsCommand.ExecuteAsync(null);

        vm.Mods.Should().HaveCount(2);
        vm.Mods.Should().AllSatisfy(m => m.DisplayName.Should().Contain("Alpha"));
    }

    [Fact]
    public async Task LoadModsCommand_ShouldUpdateStatusMessage()
    {
        var mods = new[] { MakeMod("Mod1", "1"), MakeMod("Mod2", "2") };
        var (vm, _) = CreateVm(mods);

        await vm.LoadModsCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("2");
    }

    [Fact]
    public async Task LoadModsCommand_WhenDiscoveryThrows_ShouldShowErrorMessage()
    {
        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(_ => throw new InvalidOperationException("disk error"));
        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        dialog.ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        var vm = new ModSelectViewModel(service, dialog);

        await vm.LoadModsCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("Error");
        await dialog.ReceivedWithAnyArgs(1).ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SelectedMod_WhenSet_ShouldPopulateAvailableFolders()
    {
        var mod = MakeMod("TestMod", "42");
        var folders = new[]
        {
            new ExtractableFolder(mod, "1.6/Defs", null),
            new ExtractableFolder(mod, "1.6/Keyed", null),
        };

        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(new[] { mod });
        lister.GetExtractableFolders(mod).Returns(folders);
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        var vm = new ModSelectViewModel(service, dialog);

        await vm.LoadModsCommand.ExecuteAsync(null);
        vm.SelectedMod = vm.Mods[0];

        vm.AvailableFolders.Should().HaveCount(2);
        vm.SelectedFolders.Should().HaveCount(2); // all selected by default
    }

    [Fact]
    public async Task SelectedMod_WhenChangedToNull_ShouldClearFolders()
    {
        var mod = MakeMod("TestMod", "42");
        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(new[] { mod });
        lister.GetExtractableFolders(mod).Returns(new[] { new ExtractableFolder(mod, "1.6/Defs", null) });
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        var vm = new ModSelectViewModel(service, dialog);

        await vm.LoadModsCommand.ExecuteAsync(null);
        vm.SelectedMod = vm.Mods[0];
        vm.AvailableFolders.Should().HaveCount(1);

        vm.SelectedMod = null;
        vm.AvailableFolders.Should().BeEmpty();
        vm.SelectedFolders.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectAllFoldersCommand_ShouldSelectAllFolders()
    {
        var mod = MakeMod("TestMod", "42");
        var folders = new[]
        {
            new ExtractableFolder(mod, "1.6/Defs", null),
            new ExtractableFolder(mod, "1.6/Keyed", null),
        };

        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(new[] { mod });
        lister.GetExtractableFolders(mod).Returns(folders);
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        var vm = new ModSelectViewModel(service, dialog);

        await vm.LoadModsCommand.ExecuteAsync(null);
        vm.SelectedMod = vm.Mods[0];
        vm.ClearFolderSelectionCommand.Execute(null);
        vm.SelectedFolders.Should().BeEmpty();

        vm.SelectAllFoldersCommand.Execute(null);
        vm.SelectedFolders.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClearFolderSelectionCommand_ShouldClearSelectedFolders()
    {
        var mod = MakeMod("TestMod", "42");
        var lister = Substitute.For<IModLister>();
        lister.DiscoverAll().Returns(new[] { mod });
        lister.GetExtractableFolders(mod).Returns(new[] { new ExtractableFolder(mod, "1.6/Defs", null) });
        lister.FindReferenceMods(Arg.Any<ModMetadata>()).Returns(Array.Empty<ModMetadata>());

        var logger = NullLogger<ModDiscoveryService>.Instance;
        var service = new ModDiscoveryService(lister, logger);
        var dialog = Substitute.For<IDialogService>();
        var vm = new ModSelectViewModel(service, dialog);

        await vm.LoadModsCommand.ExecuteAsync(null);
        vm.SelectedMod = vm.Mods[0];
        vm.SelectedFolders.Should().HaveCount(1);

        vm.ClearFolderSelectionCommand.Execute(null);
        vm.SelectedFolders.Should().BeEmpty();
    }

    [Fact]
    public void ModItemViewModel_DisplayName_ShouldMatchIdentifier()
    {
        var mod = MakeMod("MyMod", "999");
        var item = new ModItemViewModel(mod);

        item.DisplayName.Should().Be(mod.Identifier);
        item.Metadata.Should().Be(mod);
    }

    [Fact]
    public void ModItemViewModel_IsSelected_ShouldBeSettable()
    {
        var mod = MakeMod("MyMod", "999");
        var item = new ModItemViewModel(mod);

        item.IsSelected = true;
        item.IsSelected.Should().BeTrue();

        item.IsSelected = false;
        item.IsSelected.Should().BeFalse();
    }
}
