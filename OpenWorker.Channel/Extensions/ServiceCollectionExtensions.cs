using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Channel.Services;

namespace OpenWorker.Channel.Extensions;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddChannels(this IServiceCollection services) 
    {
        services.AddSingleton<ServiceChannels>();
        services.AddSingleton<ServerChannels>();
        
        services.AddHostedService<ChannelBootstrapService>();
        services.AddHostedService<ChannelLifecycleService>();
        
        return services;
    }
}