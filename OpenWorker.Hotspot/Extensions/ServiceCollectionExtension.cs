using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.Extensions;
using OpenWorker.Persistence;

namespace OpenWorker.Hotspot.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHotspot(this IServiceCollection services, HostBuilderContext context, EntityComponentService service)
    {
        context.Configuration[ConfigurationUtils.InstanceTargetKey] = service.ToString();
        
        return services
            .AddSingleton(x =>
            {
                var factory = x.GetRequiredService<IDbContextFactory<PersistenceContext>>();
                using var dbContext = factory.CreateDbContext();

                return new ServerContent(dbContext.ServerContents);
            })
            .AddSingleton(World.Create())
            .AddSingleton<ArchComponentProvider>()
            .AddHotspotHandlers()
            .AddHostedService<ServerService>();
    }
}