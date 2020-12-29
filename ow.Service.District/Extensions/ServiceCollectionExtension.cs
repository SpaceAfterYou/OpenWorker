using Microsoft.Extensions.DependencyInjection;
using ow.Service.District.Network.Sync.Handlers;

namespace ow.Service.District.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddHandlers(this IServiceCollection service) => service
            .AddTransient<CharacterHandler>()
            .AddTransient<ChatHandler>()
            .AddTransient<GestureHandler>()
            .AddTransient<ServiceHandler>();
    }
}