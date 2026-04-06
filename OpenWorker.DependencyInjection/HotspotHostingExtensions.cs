using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Extensions;
using OpenWorker.Persistence;

namespace OpenWorker.DependencyInjection;

public static class HotspotHostingExtensions
{
    public static IServiceCollection AddHotspot(this IServiceCollection services, HostBuilderContext context, EntityComponentService service)
    {
        context.Configuration[ConfigurationUtils.InstanceTargetKey] = service.ToString();

        return services
            .AddSingleton(_ =>
            {
                var factory = _.GetRequiredService<IDbContextFactory<PersistenceContext>>();
                using var dbContext = factory.CreateDbContext();

                return new ServerContent(dbContext.ServerContents);
            })
            .AddSingleton(World.Create())
            .AddSingleton<ArchComponentProvider>()
            .AddHotspotHandlers()
            .AddHostedInternalServices();
    }

    private static IServiceCollection AddHostedInternalServices(this IServiceCollection services)
    {
        return services.AddHostedService<ServerService>();
    }
}
