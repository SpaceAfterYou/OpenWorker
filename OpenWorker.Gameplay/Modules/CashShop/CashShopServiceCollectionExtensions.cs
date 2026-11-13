using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Gameplay.Modules.CashShop;

public static class CashShopServiceCollectionExtensions
{
    /// <summary>
    /// The products of the cash shop, and the checks of an order.
    /// </summary>
    public static IServiceCollection AddCashShop(this IServiceCollection services)
    {
        services.AddSingleton<CashShopCatalog>();
        services.AddSingleton<CashShopOrders>();

        return services;
    }
}
