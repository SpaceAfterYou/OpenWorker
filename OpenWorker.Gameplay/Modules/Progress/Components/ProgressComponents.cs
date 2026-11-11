using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Progress.Components;

/// <summary>
/// The achievements of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct AchievementComponent
{
    public int Person;

    public AchievementBook? Book;

    /// <summary>
    /// When the play time of the person was last counted (a timestamp of the clock of the manager).
    /// </summary>
    public long PlayCountedAt;

    /// <summary>
    /// The play time already seen but short of a whole minute.
    /// </summary>
    public TimeSpan Played;
}

/// <summary>
/// The points of the districts and of the groups of mazes of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct SharePointComponent
{
    public int Person;

    public SharePointBook? Book;
}
