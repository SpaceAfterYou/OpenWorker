using OpenWorker.AuthServer.App.Extensions;
using OpenWorker.AuthServer.Server.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();

        services.AddPersistence();
        
        services.AddGameplay();
        services.AddGateSyncService();
        
        services.AddHotspot(context, EntityComponentService.Auth);
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);