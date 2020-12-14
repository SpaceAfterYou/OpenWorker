using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.Network.Providers;

namespace ow.Framework.IO.Network.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddFramework(this IServiceCollection services) => services
            .AddSingleton<HandlerProvider>()
            .AddSingleton<World>()
            .AddSingleton<GameServer>()
            .AddTransient<GameSession>();
    }
}