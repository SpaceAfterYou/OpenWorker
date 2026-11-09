using OpenWorker.Channel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Channel.Extensions;
using OpenWorker.Commands.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.DistrictServer.Gameplay.Extensions;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Server.Services;
using OpenWorker.DistrictServer.Trade;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Res.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Server.Common.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .UseHealthEndpoint()
    .ConfigureServices((context, services) =>
    {
        context.Configuration[ConfigurationUtils.InstanceIdentifierKey] = Guid.CreateVersion7().ToString();

        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();
        services.AddChannelCache();
        services.AddDistrictCache();
        services.AddDistrictReserveCache();
        services.AddRes();
        services.AddBatch();

        services.AddDistrictOptions();
        services.AddPlayerTransfer();

        services.AddPersistence();
        services.AddItems();
        services.AddRewardsOptions();
        services.AddSingleton<CombatTables>();
        services.AddGameplay();
        services.AddMiscGameplay();
        services.AddQuestGameplay();

        services.AddCommandManager();

        services.AddChannels();

        services.AddSingleton<NpcManager>();
        services.Replace(ServiceDescriptor.Singleton<IQuestNpcs, DistrictQuestNpcs>());
        services.AddSingleton<INpcPlaces, DistrictNpcPlaces>();
        services.AddNpcServices();
        services.AddServerCommon<ChannelAudience>();
        services.AddSingleton<IPersonPlaceSource, PersonPlaceStore>();
        services.AddTrade();
        services.AddPartyMazeEntry();

        services.AddHotspot(context, EntityComponentService.District);

        services.AddRegistrationService<ServerBootstrapService>();
    })
    .Build()
    .RunAsync();
