using Arch.Core;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Gameplay.Modules.Rewards;

/// <summary>
/// Used while persons have no inventory: the reward counts as taken, but nothing is given.
/// </summary>
public sealed class NoRewardGranter(ILogger<NoRewardGranter> logger) : IRewardGranter
{
    public ValueTask<bool> TryGrantAsync(
        Entity player,
        Reward reward,
        RewardSource source,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "No inventory: the reward from {Source} ({Count} item(s), {Gold} gold, {Ether} ether) is not given.",
            source,
            reward.Items.Count,
            reward.Gold,
            reward.Ether);

        return ValueTask.FromResult(true);
    }
}
