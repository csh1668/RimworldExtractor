using Microsoft.Extensions.DependencyInjection;
using RimworldExtractor.Application.Extraction;
using RimworldExtractor.Domain.Abstractions;

namespace RimworldExtractor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IExtractionPipeline, NoOpExtractionPipeline>();
        return services;
    }
}
