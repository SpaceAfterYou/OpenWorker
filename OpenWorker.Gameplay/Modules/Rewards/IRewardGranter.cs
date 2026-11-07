using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Rewards;

/// <summary>
/// An item and its amount to give to a person.
/// </summary>
public readonly record struct RewardItem(int Item, short Count);

/// <summary>
/// What the reward comes from, for the logs and for rules that depend on it.
/// </summary>
public enum RewardSource : byte
{
    SoulMetry = 1,
    DailyMission = 2,
    Attendance = 3,
    Helper = 4,
    Vaccum = 5
}

/// <summary>
/// The place where features that give items hand them over. The inventory is not part of this module:
/// whoever owns it replaces <see cref="NoRewardGranter"/> with the real implementation.
/// </summary>
public interface IRewardGranter
{
    /// <summary>
    /// Gives all the items or none of them. False means they do not fit or the grant failed, and
    /// the caller must not count the reward as taken.
    /// </summary>
    ValueTask<bool> TryGrantAsync(
        Entity player,
        IReadOnlyList<RewardItem> items,
        RewardSource source,
        CancellationToken cancellationToken);
}
