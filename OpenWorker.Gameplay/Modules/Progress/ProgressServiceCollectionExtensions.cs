using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Progress;

public static class ProgressServiceCollectionExtensions
{
    /// <summary>
    /// The achievements and the share points of a person, and the documents the flush writes for them.
    /// </summary>
    public static IServiceCollection AddProgressGameplay(this IServiceCollection services)
    {
        if (services.Any(e => e.ServiceType == typeof(AchievementManager)))
        {
            return services;
        }

        services.AddSingleton<ProgressItems>();

        services.AddSingleton<AchievementCatalog>();
        services.AddSingleton<OpenTitleManager>();

        // The battle points a person gains from wherever count for the achievements, and the minutes it plays are counted on the tick.
        services.AddSingleton(provider =>
        {
            var manager = ActivatorUtilities.CreateInstance<AchievementManager>(provider);

            provider.GetService<ItemArrivals>()?.ListenBattlePoints(manager.OnBattlePointsGained);

            return manager;
        });

        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<AchievementManager>());

        services.AddSingleton<SharePointCatalog>();
        services.AddSingleton<SharePointManager>();

        services.AddSingleton(AchievementManager.Document);
        services.AddSingleton(OpenTitleManager.Document);
        services.AddSingleton(SharePointManager.Document);

        return services;
    }
}
