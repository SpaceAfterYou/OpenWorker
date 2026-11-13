using Arch.Core;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Batch;
using OpenWorker.Channel.Community;
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
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.UnityPoints;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.Lua.Managers;
using OpenWorker.Navigation;
using OpenWorker.MazeServer.Services;
using OpenWorker.Res.Extensions;
using OpenWorker.Server.Common.Extensions;

namespace OpenWorker.MazeServer;

/// <summary>
/// The host of the maze, built and not yet started.
/// </summary>
public static class MazeServerHost
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
        context.Configuration[ConfigurationUtils.InstanceIdentifierKey] = Guid.CreateVersion7().ToString();

        services.AddCache();
        services.AddSessionCache();

        // A person plays here: the next server waits until this one has let them go.
        services.Configure<SessionLeaseOptions>(options => options.Play = true);
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
        services.AddSingleton<PartyRevision>();
        services.AddSingleton<SkillAggro>();
        services.AddSingleton<GameModes>();
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
        services.AddSingleton<WorldMazeKeeper>().AddHostedService<WorldMazeRenewal>();
        services.AddMyRoom().AddSingleton<MyRooms>().AddSingleton<IMyRoomPlace, MazeRoomPlace>().AddHostedService<MyRoomKeeper>();
        services.AddSingleton<PartyMazeEvents>();
        services.AddSingleton<ICommunityModule>(provider => provider.GetRequiredService<PartyMazeEvents>());
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

        // A hidden event counts what the players of its maze do, wherever the server hears of it.
        services.AddSingleton<HiddenEventCatalog>();
        services.AddSingleton(provider =>
        {
            var world = provider.GetRequiredService<World>();

            return new HiddenEventCounter
            {
                Route = (player, target, what, amount) =>
                {
                    if (world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { IsClosed: false })
                    {
                        maze.CountHiddenEvent(target, what, amount);
                    }
                }
            };
        });
        services.AddSingleton<UnityEvents>();
        services.AddSingleton<PrologueService>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<MazeResults>());

        // When a fallen player may get up, and the revive items it gets up with.
        services.AddSingleton<MazeRevival>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<MazeRevival>());
        services.AddSingleton<IReviveItems, MazeReviveItems>();
        services.AddSingleton<ISessionCloseHandler, DropCleaner>();

        services.AddChannels();

        // services.AddSingleton<ItemSystem>();
        // services.AddSingleton<NpcManager>();

        // A player in a maze is the person of the district, with the same components.
        services.AddHotspot(context, EntityComponentService.District);
        services.AddRegistrationService<MazeRegistrationService>();

        // services.AddHostedService<ServerBootstrapService>();
    }
}
