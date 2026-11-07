using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Gameplay.Modules.Vaccum;

namespace OpenWorker.Gameplay.Extensions;

public static class MiscGameplayExtensions
{
    /// <summary>
    /// The services of the features that keep their progress in <see cref="PersonStateStore"/>. Whoever owns the
    /// inventory registers an <see cref="IRewardGranter"/> before this call to give real items.
    /// </summary>
    public static IServiceCollection AddMiscGameplay(this IServiceCollection services)
    {
        services.TryAddSingleton<IRewardGranter, NoRewardGranter>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<PersonStateStore>();
        services.AddSingleton<MiscStateLoader>();
        services.AddHostedService<MiscTickService>();

        services.AddSingleton<SoulMetryCatalog>();
        services.AddSingleton<SoulMetryManager>();

        services.AddSingleton<DailyMissionCatalog>();
        services.AddSingleton<DailyMissionManager>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<DailyMissionManager>());

        services.AddSingleton<BoosterCatalog>();
        services.AddSingleton<BoosterManager>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<BoosterManager>());

        services.TryAddSingleton<IVaccumRewards, NoVaccumRewards>();
        services.AddSingleton<VaccumManager>();

        services.AddSingleton<HelperCatalog>();
        services.AddSingleton<HelperManager>();

        services.AddSingleton<AttendanceCatalog>();
        services.AddSingleton<AttendanceManager>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<AttendanceManager>());

        return services;
    }
}
