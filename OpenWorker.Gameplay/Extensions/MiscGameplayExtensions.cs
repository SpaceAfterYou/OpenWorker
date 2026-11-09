using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Extensions;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Attendance.Components;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.DailyMissions.Components;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.SoulMetry.Components;
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
        services.AddAttendanceOptions();
        services.AddDailyMissionOptions();

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

        // What the flush of the person writes when the managers mark their part.
        services.AddSingleton<IPersonDocumentSource>(new PersonDocumentSource<SoulMetryComponent>(PersonStateKind.SoulMetry, PersonSections.MiscState, e => e.State));
        services.AddSingleton<IPersonDocumentSource>(new PersonDocumentSource<DailyMissionComponent>(PersonStateKind.DailyMission, PersonSections.MiscState, e => e.Book));
        services.AddSingleton<IPersonDocumentSource>(new PersonDocumentSource<BoosterComponent>(PersonStateKind.Booster, PersonSections.MiscState, e => e.Book));
        services.AddSingleton<IPersonDocumentSource>(new PersonDocumentSource<AttendanceComponent>(PersonStateKind.Attendance, PersonSections.MiscState, e => e.Book));
        services.AddSingleton<IPersonDocumentSource>(new PersonDocumentSource<HelperComponent>(PersonStateKind.Helper, PersonSections.MiscState, e => e.Book));

        return services;
    }
}
