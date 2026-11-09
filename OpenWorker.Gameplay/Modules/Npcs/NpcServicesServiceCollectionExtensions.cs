using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Shop;

namespace OpenWorker.Gameplay.Modules.Npcs;

public static class NpcServicesServiceCollectionExtensions
{
    /// <summary>
    /// What the NPCs of a city do for a player: the check of the NPC behind a service, the credit with groups of NPCs and the
    /// shops. A server that places NPCs replaces <see cref="INpcPlaces"/>; without it no NPC serves anything.
    /// </summary>
    public static IServiceCollection AddNpcServices(this IServiceCollection services)
    {
        services.TryAddSingleton<INpcPlaces, NoNpcPlaces>();
        services.AddSingleton<NpcCatalog>();
        services.AddSingleton<ServiceNpcs>();
        services.AddSingleton<NpcCreditTables>();
        services.AddSingleton<NpcCreditManager>();
        services.AddSingleton(NpcCreditManager.Document);
        services.AddSingleton<NpcShopService>();

        return services;
    }
}
