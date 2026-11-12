using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Tower;

public static class TowerServiceCollectionExtensions
{
    /// <summary>
    /// The infinite tower: the climb of each person, its daily count and the flush of its book.
    /// </summary>
    public static IServiceCollection AddInfiniteTower(this IServiceCollection services)
    {
        services.AddSingleton<InfiniteTower>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<InfiniteTower>());
        services.AddSingleton(InfiniteTower.Document);

        return services;
    }
}
