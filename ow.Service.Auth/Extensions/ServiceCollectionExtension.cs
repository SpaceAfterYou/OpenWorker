using Microsoft.Extensions.DependencyInjection;
using ow.Service.Auth.Network.Sync.Handlers;

namespace ow.Service.Auth.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddHandlers(this IServiceCollection service) => service
            .AddTransient<GateHandler>()
            .AddTransient<ServiceHandler>();
    }
}