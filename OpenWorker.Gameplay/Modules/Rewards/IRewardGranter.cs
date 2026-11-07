using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Rewards;

/// <summary>
/// An item and its amount to give to a person.
/// </summary>
public readonly record struct RewardItem(int Item, short Count);

/// <summary>
/// What a feature gives to a person at once: items and money.
/// </summary>
public sealed record Reward(IReadOnlyList<RewardItem> Items, int Gold = 0, int Ether = 0)
{
    public static Reward Empty { get; } = new([]);

    public bool IsEmpty => Items.Count == 0 && Gold == 0 && Ether == 0;
}

/// <summary>
/// What the reward comes from, for the logs and for rules that depend on it.
/// </summary>
public enum RewardSource : byte
{
    SoulMetry = 1,
    DailyMission = 2,
    Attendance = 3,
    Helper = 4,
    Vaccum = 5,
    PlayTime = 6
}

/// <summary>
/// The place where features that give items hand them over. The inventory is not part of this module:
/// whoever owns it replaces <see cref="NoRewardGranter"/> with the real implementation.
/// </summary>
public interface IRewardGranter
{
    /// <summary>
    /// Gives all of the reward or none of it. False means it does not fit or the grant failed, and
    /// the caller must not count the reward as taken. Some rewards are meant to go by post (daily missions
    /// do), which is up to the implementation.
    /// </summary>
    ValueTask<bool> TryGrantAsync(
        Entity player,
        Reward reward,
        RewardSource source,
        CancellationToken cancellationToken);
}
