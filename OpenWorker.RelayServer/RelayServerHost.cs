using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Persistence;
using OpenWorker.RelayServer.DependencyInjection;
using OpenWorker.RelayServer.Services;
using OpenWorker.Res.Extensions;

namespace OpenWorker.RelayServer;

/// <summary>
/// The host of the relay, built and not yet started.
/// </summary>
public static class RelayServerHost
{
    public static IHostBuilder CreateBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .UseHealthEndpoint()
            .ConfigureServices(AddServices);
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddCache();
        services.AddPersistence();
        services.AddTelemetry();
        services.AddCredentialsOptions();
        services.AddDiscord();
        services.AddRes();

        services.AddCacheBootstrapService();

        services.AddHostedService<DiscordService>();
    }
}
