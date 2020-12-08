using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Core.Systems.LanSystem.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddRedis(this IServiceCollection services) => services
            .AddSingleton((x) => ConnectionMultiplexer.Connect(x.GetRequiredService<IConfiguration>()["Redis:ConnectionString"]));
    }
}
