using OpenWorker.Batch;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Channel;
using OpenWorker.Channel.Extensions;
using OpenWorker.Commands;
using OpenWorker.Commands.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.MazeServer.Services;
using OpenWorker.Res.Extensions;
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

        services.AddSingleton<MazeResourceProvider>();
        services.AddSingleton<DistrictResourceProvider>();
        services.AddSingleton<BatchManager>();
        services.AddSingleton<BuffManager>();
        services.AddSingleton<CombatTables>();
        services.AddSingleton(provider => new AiScriptLoader(provider.GetRequiredService<ILogger<AiScriptLoader>>()));
        services.AddSingleton<MonsterBehaviorTables>();
        services.AddSingleton<NavMeshProvider>();
        services.AddSingleton<CreatureServices>();
        services.AddSingleton<IPlayerHealthResize, MazeHealthResize>();
        services.AddSingleton(_ => new MazeScripts());
        services.AddSingleton<SimulationClock>();
        services.AddHostedService(provider => provider.GetRequiredService<SimulationClock>());

        services.AddCommandManager();

        services.AddGateOptions();
        services.AddPlayerTransfer();
        services.AddServerCommon<MazeAudience>();
        services.AddSingleton<MazeInstances>();
        services.AddSingleton<PartyMazeEvents>();
        services.AddSingleton<MazeHelperField>();
        services.AddSingleton<OpenWorker.Gameplay.Modules.Helpers.ISummonedHelpers, MazeSummonedHelpers>();
        services.AddSingleton<OpenWorker.Gameplay.Modules.Helpers.IHelperField>(provider => provider.GetRequiredService<MazeHelperField>());
        services.AddSingleton<ISessionCloseHandler, MazeCloseHandler>();

        services.AddPersistence();
        services.AddMiscGameplay();

        // The townsfolk of a maze (District 6 has the NPCs of transports) serve the player the way those of a city do.
        services.AddSingleton<INpcPlaces, MazeNpcPlaces>();
        services.AddNpcServices();
        services.AddSingleton<ITransportMap, MazeTransportMap>();
        services.AddQuestGameplay();
        services.AddSingleton<MazeReporter>();

        // Items act in the maze: the field is registered before the items, which keep a district's for a server without one.
        services.AddSingleton<OpenWorker.Gameplay.Modules.Items.IItemField, MazeItemField>();
        services.AddItems();
        services.AddRewardsOptions();
        services.AddCommandOptions();
        services.AddSingleton<DropService>();
        services.AddSingleton<KillRewards>();
        services.AddSingleton<MazeKeyRewards>();
        services.AddSingleton<MazeResults>();
        services.AddSingleton<PrologueService>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<MazeResults>());

        // When a fallen player may get up, and the revive items it gets up with.
        services.AddSingleton<MazeRevival>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<MazeRevival>());
        services.AddSingleton<IReviveItems, MazeReviveItems>();
        services.AddSingleton<ISessionCloseHandler, DropCleaner>();
        services.AddHostedService<DropExpiryService>();

        services.AddChannels();

        // services.AddSingleton<ItemSystem>();
        // services.AddSingleton<NpcManager>();

        // A player in a maze is the person of the district, with the same components.
        services.AddHotspot(context, EntityComponentService.District);
        services.AddRegistrationService<MazeRegistrationService>();

        // services.AddHostedService<ServerBootstrapService>();
    })
    .Build()
    .RunAsync();
