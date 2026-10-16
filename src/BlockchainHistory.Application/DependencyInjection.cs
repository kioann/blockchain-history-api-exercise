using BlockchainHistory.Application.Common;
using BlockchainHistory.Application.Diagnostics;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlockchainHistory.Application;

public static class DependencyInjection
{
    /// <summary>Registers the mediator, its pipeline (logging, then validation), validators and metrics.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator((MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ApplicationMetrics>();

        return services;
    }
}
