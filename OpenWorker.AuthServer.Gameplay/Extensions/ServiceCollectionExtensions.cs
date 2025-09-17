using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.AuthServer.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddGameplay(this IServiceCollection services)
    {
        services.AddSingleton<LoginGameplay>();
        services.AddSingleton<GateGameplay>();
    }
}