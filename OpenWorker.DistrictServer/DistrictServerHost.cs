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
        services.AddSocialItems().AddSingleton<ISocialItemGrounds, DistrictSocialItemGrounds>().AddSingleton<NavMeshProvider>();
        services.AddHostedService<PostSweepService>();

        services.AddHotspot(context, EntityComponentService.District);

        services.AddRegistrationService<ServerBootstrapService>();
    }
}
