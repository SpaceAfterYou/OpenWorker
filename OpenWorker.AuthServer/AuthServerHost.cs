using OpenWorker.AuthServer.App.Extensions;
using OpenWorker.AuthServer.Server.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;

namespace OpenWorker.AuthServer;

/// <summary>
/// The host of the auth server, built and not yet started.
/// </summary>
public static class AuthServerHost
{
    public static IHostBuilder CreateBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .UseHealthEndpoint()
            .ConfigureServices(AddServices);
    }

    private static void AddServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();

        services.AddPersistence();

        services.AddGameplay();
        services.AddGateSyncService();

        services.AddHotspot(context, EntityComponentService.Auth);
    }
}
