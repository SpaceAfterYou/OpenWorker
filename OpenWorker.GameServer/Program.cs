using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Communication.Relay.Services;
using OpenWorker.GameServer;
using OpenWorker.GameServer.Hotspot;
using OpenWorker.GameServer.Hotspot.Handler.Extensions;
using OpenWorker.GameServer.Worlds;
using OpenWorker.GameServer.Worlds.Abstractions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(e =>
        {
            e.AddConsumer<GateService>();

            e.UsingInMemory((ctx, cfg) =>
            {
                cfg.ConfigureEndpoints(ctx);
            });
        });

        services
            .AddStackExchangeRedisCache(e =>
            {
                e.Configuration = context.Configuration.GetConnectionString("Redis");
            })
            .AddHotspotHandlers()
            .AddHostedService<ServerService>();

        var instanceType = context.Configuration.GetValue<InstanceType>("Instance:Type");
        if (instanceType != InstanceType.Game)
        {
            throw new InvalidOperationException();
        }

        var worldType = context.Configuration.GetValue<WorldType>("Instance:World:Type");
        switch (worldType)
        {
            case WorldType.Maze:
                services
                    .AddSingleton<IWorld, MazeWorld>()
                    .AddHostedService<WorldService>();
                break;

            case WorldType.District:
                throw new NotImplementedException();

            default:
                throw new ApplicationException();
        }

    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);