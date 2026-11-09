using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Gameplay.Modules.Persistence;

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
        services.AddSingleton<AchievementManager>();

        services.AddSingleton<SharePointCatalog>();
        services.AddSingleton<SharePointManager>();

        services.AddSingleton(AchievementManager.Document);
        services.AddSingleton(SharePointManager.Document);

        return services;
    }
}
