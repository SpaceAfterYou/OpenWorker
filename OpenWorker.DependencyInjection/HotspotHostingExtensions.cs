using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Extensions;

namespace OpenWorker.DependencyInjection;

public static class HotspotHostingExtensions
{
    public static IServiceCollection AddHotspot(this IServiceCollection services, HostBuilderContext context, EntityComponentService service)
    {
        context.Configuration[ConfigurationUtils.InstanceTargetKey] = service.ToString();

        return services
            .AddServerOptions()
            // Filled when the server starts, after the migration: nothing reads the database while the container is built.
            .AddSingleton<ServerContent>()
            .AddHostedService<ServerContentLoadService>()
            .AddSingleton(World.Create())
            .AddSingleton<WorldLoop>()
            .AddSingleton<ArchComponentProvider>()
            .AddHotspotHandlers()
            .AddServerHealthChecks()
            .AddTelemetry()
            .AddHostedInternalServices();
    }

    /// <summary>
    /// The listener and the drain that stops it. The drain is a lifecycle service: the host calls it before it stops
    /// any service, so what it does first does not depend on the order of the registrations.
    /// </summary>
    private static IServiceCollection AddHostedInternalServices(this IServiceCollection services)
    {
        // The host waits as long as the settings say. They are read from the configuration, not from the options:
        // the options are checked when the host starts, and an unchecked value is never used by a server that runs.
        services.AddOptions<HostOptions>().Configure<IConfiguration>((host, configuration) =>
        {
            var timeout = configuration.GetSection(ShutdownOptions.SectionName).GetValue<TimeSpan?>(nameof(ShutdownOptions.Timeout));

            if (timeout > TimeSpan.Zero)
            {
                host.ShutdownTimeout = timeout.Value;
            }
        });

        return services
            .AddSingleton<ServerState>()
            .AddSingleton<SessionTracker>()
            .AddSingleton<ServerService>()
            .AddHostedService(e => e.GetRequiredService<ServerService>())
            .AddHostedService<ServerShutdownService>();
    }
}
