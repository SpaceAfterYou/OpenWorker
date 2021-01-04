using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.Network.CS;
using StackExchange.Redis;

namespace ow.Framework.IO.Lan.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddLan(this IServiceCollection services) => services
            .AddTransient((x) => ConnectionMultiplexer.Connect(x.GetRequiredService<IConfiguration>()["Redis:ConnectionString"]))
            .AddSingleton<CSClient>();
    }
}