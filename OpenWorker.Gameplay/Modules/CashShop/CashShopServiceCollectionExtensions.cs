using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Gameplay.Modules.CashShop;

public static class CashShopServiceCollectionExtensions
{
    /// <summary>
    /// The products of the cash shop, the checks of an order and the gacha items.
    /// </summary>
    public static IServiceCollection AddCashShop(this IServiceCollection services)
    {
        services.AddSingleton<CashShopCatalog>();
        services.AddSingleton<CashShopOrders>();
        services.AddSingleton<CashShopGacha>();

        return services;
    }
}
