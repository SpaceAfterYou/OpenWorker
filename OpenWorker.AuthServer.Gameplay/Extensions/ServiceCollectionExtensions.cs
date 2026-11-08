using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Extensions;

namespace OpenWorker.AuthServer.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddGameplay(this IServiceCollection services)
    {
        services.AddAuthOptions();

        services.AddSingleton<LoginGameplay>();
        services.AddSingleton<GateGameplay>();
    }
}