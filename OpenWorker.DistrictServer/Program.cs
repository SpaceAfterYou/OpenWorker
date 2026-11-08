using OpenWorker.Channel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Channel.Extensions;
using OpenWorker.Commands.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DistrictServer.Gameplay.Extensions;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Server.Services;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
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
        services.AddSingleton<PersonRegistry>();
        services.AddSingleton<ISessionCloseHandler, PersonPlaceStore>();

        services.AddHotspot(context, EntityComponentService.District);

        services.AddHostedService<ServerBootstrapService>();
        services.AddHostedService<KeepAliveService>();
    })
    .Build()
    .RunAsync();
