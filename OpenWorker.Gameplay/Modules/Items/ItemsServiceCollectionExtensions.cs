using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.State;
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
        services.AddSingleton<ItemArrivals>();
        services.AddSingleton<CostumeBroachPlaces>();
        services.AddSingleton<DatabaseItemSerials>();
        services.AddSingleton<IItemSerials>(provider => provider.GetRequiredService<DatabaseItemSerials>());
        services.AddSingleton<InventoryFactory>();
        services.AddSingleton<GearStats>();
        services.AddSingleton<ItemOptionRoller>();
        services.AddSingleton<PlayerStatsService>();
        services.AddSingleton<EnduranceWear>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<ItemUseLimits>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<ItemUseLimits>());
        services.AddSingleton(ItemUseDocument.Source);

        // Where an item acts and who gives its title: the maze and the layer above the titles say, a server with neither
        // answers as a city with no titles does.
        services.TryAddSingleton<IItemField, DistrictItemField>();
        services.TryAddSingleton<IItemTitles, NoItemTitles>();
        services.AddSingleton<ItemUseService>();
        services.AddSingleton<ItemRepairService>();
        services.AddSingleton<ItemSetupAnswers>();
        services.AddSingleton<ItemRestoreService>();
        services.AddSingleton<ItemSocketService>();
        services.AddSingleton<ItemUpgradeService>();
        services.AddSingleton<ItemMakeService>();
        services.AddSingleton<ItemExchangeService>();
        services.AddSingleton<ItemDisassembleService>();
        services.AddSingleton<ItemEvolutionService>();
        services.AddSingleton<AkashicService>();
        services.AddSingleton<ItemBroachService>();
        services.AddSingleton<InventoryPersistence>();

        // The features that give items hand them over here.
        services.Replace(ServiceDescriptor.Singleton<IRewardGranter, InventoryRewardGranter>());

        // Quests count and take the items of the bags.
        services.Replace(ServiceDescriptor.Singleton<IQuestItems, InventoryQuestItems>());
        services.AddSingleton<ExperienceService>();

        // Items, the wallet and the rest of the person are written by the flush of the person.
        services.AddPersonPersistence();
        services.AddSingleton(AkashicRecordBook.Document);
        services.AddPersonSkills();

        return services;
    }
}
