using FluentAssertions;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Plugins;

namespace RimworldExtractor.Application.Tests.Compat;

public class CompatRegistryTests
{
    [CompatPriority(50)]
    private sealed class HighPriorityPlugin : CompatPluginBase
    {
        public override string Name => "High";
    }

    [CompatPriority(150)]
    private sealed class LowPriorityPlugin : CompatPluginBase
    {
        public override string Name => "Low";
    }

    private sealed class DefaultPriorityPlugin : CompatPluginBase
    {
        public override string Name => "Default";
    }

    [Fact]
    public void Ordered_SortsPluginsByPriorityAscending()
    {
        ICompatPlugin[] plugins = [new LowPriorityPlugin(), new HighPriorityPlugin()];
        var registry = new CompatRegistry(plugins);

        registry.Ordered.Should().HaveCount(2);
        registry.Ordered[0].Name.Should().Be("High");
        registry.Ordered[1].Name.Should().Be("Low");
    }

    [Fact]
    public void Ordered_PluginWithNoAttribute_GetsDefaultPriority100()
    {
        ICompatPlugin[] plugins = [new LowPriorityPlugin(), new DefaultPriorityPlugin(), new HighPriorityPlugin()];
        var registry = new CompatRegistry(plugins);

        registry.Ordered.Select(p => p.Name).Should().Equal("High", "Default", "Low");
    }
}
