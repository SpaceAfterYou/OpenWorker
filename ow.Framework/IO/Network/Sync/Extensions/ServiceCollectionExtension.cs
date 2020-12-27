using Microsoft.Extensions.DependencyInjection;
using ow.Framework.IO.Network.Sync.Providers;

namespace ow.Framework.IO.Network.Sync.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddFramework(this IServiceCollection services) => services
            .AddSingleton<HandlerProvider>();
    }
}