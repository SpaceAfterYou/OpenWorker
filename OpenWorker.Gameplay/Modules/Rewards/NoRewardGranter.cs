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
        IReadOnlyList<RewardItem> items,
        RewardSource source,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("No inventory: {Count} reward item(s) from {Source} are not given.", items.Count, source);

        return ValueTask.FromResult(true);
    }
}
