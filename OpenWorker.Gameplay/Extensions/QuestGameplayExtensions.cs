using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.Gameplay.Extensions;

public static class QuestGameplayExtensions
{
    /// <summary>
    /// The services of quests. They keep their progress in the store of <see cref="MiscGameplayExtensions.AddMiscGameplay"/>,
    /// which has to be added too. Whoever owns the inventory registers an <see cref="IQuestItems"/> before this call.
    /// </summary>
    public static IServiceCollection AddQuestGameplay(this IServiceCollection services)
    {
        services.TryAddSingleton<IQuestItems, NoQuestItems>();
        services.TryAddSingleton<IQuestStore, PersonStateQuestStore>();

        services.AddSingleton<QuestCatalog>();
        services.AddSingleton<QuestManager>();
        services.AddSingleton(QuestManager.Document);

        return services;
    }
}
