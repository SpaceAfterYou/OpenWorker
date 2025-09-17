using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.DistrictServer.Gameplay.Extensions;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddGameplay(this IServiceCollection services) 
    {
        services.AddSingleton<GestureGameplay>();
        
        return services;
    }
}