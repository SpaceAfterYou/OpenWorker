using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;

namespace OpenWorker.DistrictServer.Trade;

public static class TradeServiceCollectionExtensions
{
    /// <summary>
    /// The trades and the private shops of the district, the expiry of the invitations and the close handler. The close
    /// handler goes first of all close handlers: the trade and the shop of a leaving player end before its last flush takes
    /// its snapshot, so another player cannot move anything of it while that flush waits for the database.
    /// </summary>
    public static IServiceCollection AddTrade(this IServiceCollection services)
    {
        services.AddSingleton<TradeGoods>();
        services.AddSingleton<Trades>();
        services.AddSingleton<PrivateShops>();
        services.AddSingleton<IItemLocks>(provider => new TradeItemLocks(
            provider.GetRequiredService<Trades>(),
            new Lazy<PrivateShops>(provider.GetRequiredService<PrivateShops>)));
        services.AddSingleton<TradeCloseHandler>();
        services.Insert(0, ServiceDescriptor.Singleton<ISessionCloseHandler>(provider => provider.GetRequiredService<TradeCloseHandler>()));
        services.AddHostedService<TradeExpiryService>();

        return services;
    }
}
