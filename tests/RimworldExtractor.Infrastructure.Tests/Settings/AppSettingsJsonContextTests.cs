using System.Text.Json;
using FluentAssertions;
using RimworldExtractor.Domain.Settings;
using RimworldExtractor.Infrastructure.Settings;

namespace RimworldExtractor.Infrastructure.Tests.Settings;

public class AppSettingsJsonContextTests
{
    [Fact]
    public void Serialize_Default_ProducesJson()
    {
        var json = JsonSerializer.Serialize(AppSettings.Default, AppSettingsJsonContext.Default.AppSettings);

        json.Should().Contain("\"schemaVersion\": 2");
        json.Should().Contain("\"original\":");
    }

    [Fact]
    public void RoundTrip_DefaultSettings_PreservesValue()
    {
        var original = AppSettings.Default;
        var json = JsonSerializer.Serialize(original, AppSettingsJsonContext.Default.AppSettings);

        var deserialized = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);

        deserialized.Should().NotBeNull();
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_IsIndented()
    {
        var json = JsonSerializer.Serialize(AppSettings.Default, AppSettingsJsonContext.Default.AppSettings);

        json.Should().Contain("\n", "indented JSON has newlines");
    }
}
