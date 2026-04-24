using System.Text.Json;
using FluentAssertions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Settings;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _settingsPath;

    public JsonSettingsStoreTests()
    {
        _tmpDir = Directory.CreateTempSubdirectory("rwx-settings-").FullName;
        _settingsPath = Path.Combine(_tmpDir, "settings.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, recursive: true); } catch { /* swallow */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoadAsync_WhenFileAbsent_ReturnsDefault()
    {
        var store = new JsonSettingsStore(_settingsPath);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        loaded.Should().Be(AppSettings.Default);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsEqual()
    {
        var store = new JsonSettingsStore(_settingsPath);
        var original = AppSettings.Default with
        {
            Paths = PathSettings.Default with { Rimworld = "/rw" }
        };

        await store.SaveAsync(original, TestContext.Current.CancellationToken);
        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        loaded.Should().Be(original);
    }

    [Fact]
    public async Task SaveAsync_CreatesBackupOfPrevious()
    {
        var store = new JsonSettingsStore(_settingsPath);
        var v1 = AppSettings.Default with { Paths = PathSettings.Default with { Rimworld = "/rw1" } };
        var v2 = AppSettings.Default with { Paths = PathSettings.Default with { Rimworld = "/rw2" } };

        await store.SaveAsync(v1, TestContext.Current.CancellationToken);
        await store.SaveAsync(v2, TestContext.Current.CancellationToken);

        var backupPath = _settingsPath + ".bak";
        File.Exists(backupPath).Should().BeTrue();
        var backupText = await File.ReadAllTextAsync(backupPath, TestContext.Current.CancellationToken);
        backupText.Should().Contain("/rw1");
    }

    [Fact]
    public async Task SaveAsync_DoesNotLeaveTempFile()
    {
        var store = new JsonSettingsStore(_settingsPath);
        await store.SaveAsync(AppSettings.Default, TestContext.Current.CancellationToken);

        File.Exists(_settingsPath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithCorruptFile_Throws()
    {
        await File.WriteAllTextAsync(_settingsPath, "{{{ not valid json", TestContext.Current.CancellationToken);
        var store = new JsonSettingsStore(_settingsPath);

        Func<Task> act = () => store.LoadAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }
}
