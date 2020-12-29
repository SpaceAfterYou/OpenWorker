using Microsoft.Extensions.DependencyInjection;
using ow.Service.Gate.Network.Handlers;

namespace ow.Service.Gate.Extensions
{
    internal static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddHandlers(this IServiceCollection service) => service
            .AddTransient<CharacterHandler>()
            .AddTransient<ServiceHandler>();
    }
}