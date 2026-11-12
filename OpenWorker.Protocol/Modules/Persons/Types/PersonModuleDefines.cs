namespace OpenWorker.Hotspot.Modules.Persons.Types;

public static class PersonModuleDefines
{
    public const int MaxNameLength = 21;
    public const int MaxCommunityCommentLength = 51;
    public const byte DefaultLevel = 25;
    public const int MaxCommunityMemoLength = 31;

    /// <summary>
    /// The bits of the rewarded achievements, one per achievement: the client always reads this many bytes.
    /// </summary>
    public const int AchievementRewardBytes = 128;

    /// <summary>
    /// The groups of achievements whose counts the client reads after the achievements, always this many.
    /// </summary>
    public const int AchievementCategories = 7;
}