using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenWorker.DependencyInjection;

public static class Cache
{
    /// <summary>
    /// The Redis of <c>compose.yaml</c>, used when <c>ConnectionStrings:Redis</c> is not set.
    /// </summary>
    private const string DefaultRedis = "redis://redis:6379";

    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRedisConnectionProvider>(provider => new RedisConnectionProvider(
                provider.GetRequiredService<IConfiguration>().GetConnectionString("Redis") is { Length: > 0 } value ? value : DefaultRedis))
            .AddCacheCollection<MazeReserveCache>()
            .AddCacheCollection<MazeCache>()
            .AddCacheCollection<ChannelChatCache>();
    }

    public static IServiceCollection AddSessionCache(this IServiceCollection services)
    {
        services.AddSingleton<SessionLeaseManager>();
        services.AddSingleton<ISessionCloseHandler>(e => e.GetRequiredService<SessionLeaseManager>());
        services.AddHostedService<SessionLeaseRenewalService>();

        return services.AddCacheCollection<SessionCache>();
    }

    public static IServiceCollection AddGateCache(this IServiceCollection services)
    {
        return services.AddCacheCollection<GateCache>();
    }

    public static IServiceCollection AddDistrictCache(this IServiceCollection services)
    {
        return services.AddCacheCollection<DistrictCache>();
    }
    
    public static IServiceCollection AddDistrictReserveCache(this IServiceCollection services)
    {
        return services.AddCacheCollection<DistrictReserveCache>();
    }

    public static IServiceCollection AddChannelCache(this IServiceCollection services)
    {
        return services.AddCacheCollection<ChannelCache>();
    }
    
    public static IServiceCollection AddLocationCache(this IServiceCollection services)
    {
        return services.AddCacheCollection<LocationCache>();
    }

    /// <summary>
    /// Creates a collection that is safe to share between connections: with saved state it remembers
    /// every record it has read in dictionaries without locks, which breaks under concurrent access
    /// and never shrinks.
    /// </summary>
    public static IRedisCollection<T> CreateCollection<T>(IRedisConnectionProvider provider) where T : notnull
    {
        return provider.RedisCollection<T>(saveState: false);
    }

    private static IServiceCollection AddCacheCollection<T>(this IServiceCollection services) where T : notnull
    {
        return services.AddSingleton(e => CreateCollection<T>(e.GetRequiredService<IRedisConnectionProvider>()));
    }
}
