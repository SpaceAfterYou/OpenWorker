using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Gameplay.Extensions;

public static class QuestGameplayExtensions
{
    /// <summary>
    /// The services of quests. They keep their progress in the store of <see cref="MiscGameplayExtensions.AddMiscGameplay"/>,
    /// which has to be added too. Whoever owns the inventory registers an <see cref="IQuestItems"/>, whoever places the
    /// characters of a map an <see cref="IQuestNpcs"/> and whoever keeps titles an <see cref="IQuestTitles"/>; without them
    /// nobody has items, every character may be anywhere and titles are only told.
    /// </summary>
    public static IServiceCollection AddQuestGameplay(this IServiceCollection services)
    {
        services.TryAddSingleton<IQuestItems, NoQuestItems>();
        services.TryAddSingleton<IQuestStore, PersonStateQuestStore>();
        services.TryAddSingleton<IQuestNpcs, NoQuestNpcs>();
        services.TryAddSingleton<IQuestTitles, NoQuestTitles>();

        services.AddSingleton<QuestCatalog>();

        // The quests hear of every item that comes into the bags, for the conditions that ask to collect it.
        services.AddSingleton(provider =>
        {
            var manager = ActivatorUtilities.CreateInstance<QuestManager>(provider);

            provider.GetService<ItemArrivals>()?.Listen(manager.OnItemsArrived);

            return manager;
        });

        services.AddSingleton(QuestManager.Document);

        return services;
    }
}
