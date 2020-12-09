using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.Network.Providers;

namespace ow.Framework.IO.Network.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddNetwork(this IServiceCollection services) => services
            .AddSingleton<HandlerProvider>();
    }
}