using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application.Compat;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Application.Extraction.Stages;
using RimworldExtractor.Application.ModDiscovery;
using RimworldExtractor.Domain.Abstractions;
using RimworldExtractor.Plugins;
using RimworldExtractor.Plugins.BuiltIn;

namespace RimworldExtractor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // INodeReplacementRuleProvider: reads from ISettingsStore at resolution time (sync-over-async)
        services.AddSingleton<INodeReplacementRuleProvider>(sp =>
        {
            var store = sp.GetRequiredService<ISettingsStore>();
            var settings = store.LoadAsync().GetAwaiter().GetResult();
            return new NodeReplacementRuleProvider(settings.Extraction.NodeReplacements);
        });

        // Compat plugins — registration order irrelevant; sort happens in CompatRegistry
        services.AddCompatPlugin<NodeReplacementCompatPlugin>();
        services.AddCompatPlugin<MvcfCompatPlugin>();
        services.AddCompatPlugin<VerbCompatPlugin>();
        services.AddCompatPlugin<FactionDefCompatPlugin>();
        services.AddCompatPlugin<NoTranslateCompatPlugin>();
        services.AddCompatPlugin<ScenarioDefCompatPlugin>();
        services.AddCompatPlugin<AncientMarketLibraryCompatPlugin>();

        services.AddSingleton<CompatRegistry>();

        // Stages — registration order MATTERS for pipeline execution order.
        services.AddSingleton<IExtractionStage, LoadReferenceDefsStage>();
        services.AddSingleton<IExtractionStage, ApplyPrePatchesStage>();
        services.AddSingleton<IExtractionStage, ResolveInheritanceStage>();
        services.AddSingleton<IExtractionStage, CompatPreProcessStage>();
        services.AddSingleton<IExtractionStage, ExtractDefsStage>();
        services.AddSingleton<IExtractionStage, ExtractKeyedStage>();
        services.AddSingleton<IExtractionStage, ExtractStringsStage>();
        services.AddSingleton<IExtractionStage, ExtractPatchesStage>();
        services.AddSingleton<IExtractionStage, CompatPostProcessStage>();

        // Replace NoOp with real pipeline
        services.AddSingleton<IExtractionPipeline, ExtractionPipeline>();

        services.AddSingleton<ModDiscoveryService>();

        return services;
    }
}
