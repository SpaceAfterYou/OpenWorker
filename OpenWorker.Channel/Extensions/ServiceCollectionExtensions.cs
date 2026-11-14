using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.League;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Channel.Community;
using OpenWorker.Channel.Services;

using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.Channel.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChannels(this IServiceCollection services)
    {
        services.AddInstanceOptions();
        services.AddGateOptions();
        services.AddChannelOptions();
        services.AddWorldOptions();

        services.AddSingleton<ServiceChannels>();
        services.AddSingleton<ISessionCloseHandler>(e => e.GetRequiredService<ServiceChannels>());
        services.AddSingleton<ServerChannels>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MovementPace>();

        services.AddHostedService<ChannelBootstrapService>();
        services.AddRegistrationService<ChannelLifecycleService>();

        return services.AddCommunity();
    }

    /// <summary>
    /// What sends a player to another server: <see cref="WorldManager"/> finds the district or the maze,
    /// <see cref="LeaveManager"/> the server list or the character selection. They need the gate of the server
    /// and the addresses of the auth server and of the maze servers.
    /// </summary>
    public static IServiceCollection AddPlayerTransfer(this IServiceCollection services)
    {
        services.AddGateOptions();
        services.AddAuthEndpointOptions();
        services.AddMazeEndpointOptions();

        services.AddSingleton<WorldManager>();
        services.AddSingleton<ServerChanges>();
        services.AddSingleton<LeaveManager>();

        // A server with the write-behind of its players (AddPersonPersistence) writes them before they go; the gate keeps nothing.
        services.TryAddSingleton<IPersonFlush>(NoPersonFlush.Instance);

        return services;
    }

    /// <summary>
    /// What lets the people of one server reach the people of the others.
    /// </summary>
    private static IServiceCollection AddCommunity(this IServiceCollection services)
    {
        // The same Redis, and the same connection, the cache uses.
        services.AddRedisConnection();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICommunityTransport, RedisCommunityTransport>();
        services.AddSingleton<IPresenceStore, RedisPresenceStore>();
        services.AddSingleton<IPresenceSource, PersonPresenceSource>();
        services.AddSingleton<CommunityNetwork>();
        services.AddSingleton<ISessionCloseHandler>(e => e.GetRequiredService<CommunityNetwork>());
        services.AddHostedService<CommunityService>();

        services.AddSingleton<ChatThrottle>();
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<ChatThrottle>());

        services.AddSingleton<PartyBook>();
        services.AddSingleton<IPartyRoster>(e => e.GetRequiredService<PartyBook>());
        services.AddSingleton<PartyNotifier>();
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<PartyNotifier>());
        services.AddSingleton<ICommunityModule, PartyMemberSync>();
        services.AddHostedService<PartyUpkeepService>();
        services.AddSingleton<PartyRecruitBoard>();
        services.AddSingleton<PartyAwaiterBoard>();
        services.AddSingleton<PartyRecruitNotifier>();
        services.AddSingleton<IPartyJoinListener>(e => e.GetRequiredService<PartyRecruitNotifier>());
        services.AddSingleton<IPartyMasterListener>(e => e.GetRequiredService<PartyRecruitNotifier>());
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<PartyRecruitNotifier>());
        services.AddSingleton<IPartyClock>(e => e.GetRequiredService<PartyRecruitNotifier>());
        services.AddSingleton<PartyMatching>();
        services.AddSingleton<IPartyClock>(e => e.GetRequiredService<PartyMatching>());
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<PartyMatching>());
        services.AddSingleton<LeagueBook>();
        services.AddSingleton<ILeagueRoster>(e => e.GetRequiredService<LeagueBook>());
        services.AddSingleton<LeagueSnapshots>();
        services.AddSingleton<LeagueNotifier>();
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<LeagueNotifier>());
        services.AddSingleton<ICommunityModule>(e => e.GetRequiredService<LeagueNotifier>());
        services.AddSingleton<LeagueLevels>();
        services.AddSingleton<ILeagueTreasury, LeagueTreasury>();
        services.AddSingleton<LeagueGrowth>();
        services.AddSingleton<IMegaphoneSource, InventoryMegaphone>();

        services.AddSingleton<FriendBook>();
        services.AddSingleton<IPersonRelations>(e => e.GetRequiredService<FriendBook>());
        services.AddSingleton<FriendRecruitBoard>();
        services.AddSingleton<FriendNotifier>();
        services.AddSingleton<ICommunityListener>(e => e.GetRequiredService<FriendNotifier>());

        services.AddSingleton<TitleCatalog>();
        services.AddSingleton<TitleBook>();

        // The district registers its own copy where it needs more than the tables.
        services.TryAddSingleton<CombatTables>();
        services.AddSingleton<OtherPersonService>();
        services.AddSingleton<ICommunityModule>(e => e.GetRequiredService<OtherPersonService>());

        return services;
    }
}
