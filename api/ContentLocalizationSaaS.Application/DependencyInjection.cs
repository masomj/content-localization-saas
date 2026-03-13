using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContentLocalizationSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-level services here as the codebase grows.
        return services;
    }
}

