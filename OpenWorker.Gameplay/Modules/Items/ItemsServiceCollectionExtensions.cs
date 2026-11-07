using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Items;

public static class ItemsServiceCollectionExtensions
{
    /// <summary>
    /// The items of a game server: tables, rules, the service everything else gives and takes items through, and saving.
    /// </summary>
    public static IServiceCollection AddItems(this IServiceCollection services)
    {
        services.AddSingleton<ItemCatalog>();
        services.AddSingleton<DatabaseItemSerials>();
        services.AddSingleton<IItemSerials>(provider => provider.GetRequiredService<DatabaseItemSerials>());
        services.AddSingleton<InventoryFactory>();
        services.AddSingleton<PlayerStatsService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<ItemUseService>();
        services.AddSingleton<InventoryPersistence>();
        services.AddSingleton<ISessionCloseHandler, InventorySaver>();
        services.AddHostedService<InventoryFlushService>();

        return services;
    }
}
