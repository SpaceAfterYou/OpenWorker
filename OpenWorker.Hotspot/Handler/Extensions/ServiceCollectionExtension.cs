using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Hotspot.Handler.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHotspotHandlers(this IServiceCollection @this)
    {
        foreach (var handler in HandlerCollectionBuilder.InDomain)
        {
            @this.AddSingleton(handler);
        }

        return @this
            .AddTransient<HandlerBuilder>()
            .AddTransient<HandlerCollectionBuilder>()

            .AddSingleton<EmptyHandler>()
            .AddSingleton<HandlerExecutor>()
            .AddSingleton<HandlerCollection>();
    }
}