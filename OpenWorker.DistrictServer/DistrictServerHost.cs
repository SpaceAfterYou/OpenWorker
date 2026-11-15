using OpenWorker.Hotspot.Cache;
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
using OpenWorker.Extensions.Data;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Navigation;
using OpenWorker.Res.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Server.Common.Extensions;

namespace OpenWorker.DistrictServer;

/// <summary>
/// The host of the district, built and not yet started.
/// </summary>
public static class DistrictServerHost
{
    /// <summary>
    /// The game data the district reads.
    /// </summary>
    public static IReadOnlyList<DataNeed> Data { get; } =
    [
        new DataNeed(DataFolders.ClientTables, "the server reads every client table at start", Refuse: true),
        new DataNeed(DataFolders.ServerTables, "what only the server's tables give is missing: drops of boxes, item rolls, crafts, targets of achievements"),
        new DataNeed(DataFolders.Maps, "no player can enter the district: its start points are on its map", Refuse: true),
        new DataNeed(DataFolders.NavMeshes, "social items are placed without a check of the floor")
    ];

    public static IHostBuilder CreateBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .UseHealthEndpoint()
            .ConfigureServices(AddServices);
    }

    private static void AddServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddDataCheck(Data);
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

        services.AddDistrictOptions();
        services.AddPlayerTransfer();

        services.AddPersistence();
        services.AddItems();
        services.AddRewardsOptions();
        services.AddCommandOptions();
        services.AddSingleton<CombatTables>();
        services.AddDistrictBuffs();
        services.AddGameplay();
        services.AddMiscGameplay();
        services.AddRankingPeriods();
        services.AddQuestGameplay();

        services.AddCommandManager();

        services.AddChannels();

        services.AddSingleton<NpcManager>();
        services.Replace(ServiceDescriptor.Singleton<IQuestNpcs, DistrictQuestNpcs>());
        services.AddSingleton<INpcPlaces, DistrictNpcPlaces>();
        services.AddNpcServices();
        services.AddCashShop();
        services.AddServerCommon<ChannelAudience>();
        services.AddSingleton<IPersonPlaceSource, PersonPlaceStore>();
        services.AddTrade();
        services.AddPartyMazeEntry();
        services.AddMyRoom().AddSingleton<IMyRoomPlace, DistrictRoomPlace>();
        services.AddSocialItems().AddSingleton<ISocialItemGrounds, DistrictSocialItemGrounds>().AddSingleton(_ => new NavMeshProvider(DataFolders.NavMeshes.Path));
        services.AddHostedService<PostSweepService>();
        services.AddHostedService<CashShopSweepService>();
        services.AddHostedService<RewardLetterRecovery>();

        services.AddHotspot(context, EntityComponentService.District);

        services.AddRegistrationService<ServerBootstrapService>();
    }
}
