using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Contracts;

namespace OpenWorker.DependencyInjection;

public static class Cache
{
    public static IServiceCollection AddCache(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRedisConnectionProvider>(new RedisConnectionProvider("redis://redis:6379"))
            .AddCacheCollection<MazeReserveCache>()
            .AddCacheCollection<ChannelChatCache>();
    }

    public static IServiceCollection AddSessionCache(this IServiceCollection services)
    {
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

    private static IServiceCollection AddCacheCollection<T>(this IServiceCollection services) where T : notnull
    {
        return services.AddSingleton(e => e
            .GetRequiredService<IRedisConnectionProvider>()
            .RedisCollection<T>());
    }
}