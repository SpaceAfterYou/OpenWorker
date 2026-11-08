using Arch.Core;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Gameplay.Modules.Rewards;

/// <summary>
/// Used while persons have no inventory: the reward counts as taken, but nothing is given.
/// </summary>
public sealed partial class NoRewardGranter(ILogger<NoRewardGranter> logger) : IRewardGranter
{
    public ValueTask<bool> TryGrantAsync(
        Entity player,
        Reward reward,
        RewardSource source,
        CancellationToken cancellationToken)
    {
        LogNotGiven(source, reward.Items.Count, reward.Gold, reward.Ether);

        return ValueTask.FromResult(true);
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No inventory: the reward from {Source} ({Count} item(s), {Gold} gold, {Ether} ether) is not given.")]
    private partial void LogNotGiven(RewardSource source, int count, int gold, int ether);
}
