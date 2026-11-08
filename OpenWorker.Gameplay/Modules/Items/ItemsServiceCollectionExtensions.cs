using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Items;

public static class ItemsServiceCollectionExtensions
{
    /// <summary>
    /// The items of a game server: tables, rules, the service everything else gives and takes items through, and the
    /// write-behind of the person (<see cref="PersistenceServiceCollectionExtensions.AddPersonPersistence"/>).
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

        // The features that give items hand them over here.
        services.Replace(ServiceDescriptor.Singleton<IRewardGranter, InventoryRewardGranter>());

        // Quests count and take the items of the bags.
        services.Replace(ServiceDescriptor.Singleton<IQuestItems, InventoryQuestItems>());
        services.AddSingleton<ExperienceService>();

        // Items, the wallet and the rest of the person are written by the flush of the person.
        services.AddPersonPersistence();
        services.AddPersonSkills();

        return services;
    }
}
