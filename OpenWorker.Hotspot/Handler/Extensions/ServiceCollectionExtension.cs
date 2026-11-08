using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenWorker.Hotspot.Handler.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHotspotHandlers(this IServiceCollection @this)
    {
        foreach (var handler in HandlerCollectionBuilder.InDomain)
        {
            @this.AddSingleton(handler);
        }

        // The limits are the defaults of the record until the configuration of the servers sets them.
        @this.TryAddSingleton(TimeProvider.System);
        @this.TryAddSingleton(new HandlerErrorBudgetOptions());

        return @this
            .AddTransient<HandlerBuilder>()
            .AddTransient<HandlerCollectionBuilder>()

            .AddSingleton<EmptyHandler>()
            .AddSingleton<HandlerErrorBudget>()
            .AddSingleton<HandlerExecutor>()
            .AddSingleton<HandlerCollection>();
    }
}