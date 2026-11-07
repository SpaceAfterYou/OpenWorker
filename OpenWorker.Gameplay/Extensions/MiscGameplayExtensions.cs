using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Gameplay.Modules.State;

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

        services.AddSingleton<SoulMetryCatalog>();
        services.AddSingleton<SoulMetryManager>();

        services.AddSingleton<DailyMissionCatalog>();
        services.AddSingleton<DailyMissionManager>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<DailyMissionManager>());

        services.AddSingleton<AttendanceCatalog>();
        services.AddSingleton<AttendanceManager>();
        services.AddSingleton<IMiscTick>(provider => provider.GetRequiredService<AttendanceManager>());

        return services;
    }
}
