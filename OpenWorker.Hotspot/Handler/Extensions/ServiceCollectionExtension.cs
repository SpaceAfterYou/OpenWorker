using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Extensions;

namespace OpenWorker.Hotspot.Handler.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHotspotHandlers(this IServiceCollection @this)
    {
        foreach (var handler in HandlerTableBuilder.InDomain)
        {
            @this.AddSingleton(handler);
        }

        // The limits of the error budget come from the Hotspot:ErrorBudget section.
        @this.TryAddSingleton(TimeProvider.System);
        @this.AddHandlerErrorBudgetOptions();

        return @this
            .AddTransient<HandlerBuilder>()
            .AddTransient<HandlerTableBuilder>()

            .AddSingleton<EmptyHandler>()
            .AddSingleton<HandlerErrorBudget>()
            .AddSingleton<HandlerExecutor>()
            .AddSingleton<HandlerTable>();
    }
}