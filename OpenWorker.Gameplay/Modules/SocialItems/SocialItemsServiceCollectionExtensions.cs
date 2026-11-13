using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.SocialItems;

public static class SocialItemsServiceCollectionExtensions
{
    /// <summary>
    /// The social objects of a district's channels. The server gives the ground of its map (<see cref="ISocialItemGrounds"/>) and a
    /// <see cref="SimulationClock"/> their time runs on.
    /// </summary>
    public static IServiceCollection AddSocialItems(this IServiceCollection services)
    {
        services.AddSingleton<SocialItems>();
        services.AddSingleton<ISessionCloseHandler>(provider => provider.GetRequiredService<SocialItems>());

        return services;
    }
}
