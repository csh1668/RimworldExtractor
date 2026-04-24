using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Plugins;

namespace RimworldExtractor.Application.Tests.Plugins;

public class PluginRegistrationTests
{
    [CompatPriority(50)]
    private sealed class EarlyPlugin : CompatPluginBase
    {
        public override string Name => "Early";
    }

    [CompatPriority(150)]
    private sealed class LatePlugin : CompatPluginBase
    {
        public override string Name => "Late";
    }

    [Fact]
    public void AddCompatPlugin_RegistersAsICompatPlugin()
    {
        var services = new ServiceCollection();
        services.AddCompatPlugin<EarlyPlugin>();
        services.AddCompatPlugin<LatePlugin>();

        var provider = services.BuildServiceProvider();
        var plugins = provider.GetServices<ICompatPlugin>().ToList();

        plugins.Should().HaveCount(2);
        plugins.Should().Contain(p => p.Name == "Early");
        plugins.Should().Contain(p => p.Name == "Late");
    }

    [Fact]
    public void CompatPriorityAttribute_DefaultPriority_Is100()
    {
        var attr = new CompatPriorityAttribute();
        attr.Priority.Should().Be(100);
    }

    [Fact]
    public void CompatPluginBase_DefaultName_IsTypeName()
    {
        var plugin = new EarlyPlugin();
        plugin.Name.Should().Be("Early");
    }
}
