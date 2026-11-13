using Arch.Core;

namespace OpenWorker.Gameplay.Modules.HiddenEvents;

/// <summary>
/// Where what a player does outside the fight of a maze reaches the hidden event of the maze it plays: money and souls picked
/// up, items used. The maze server tells it how to find the maze of a player; elsewhere nothing counts.
/// </summary>
public sealed class HiddenEventCounter
{
    /// <summary>
    /// Counts for the maze of the player: what happened, the monster or item, and the amount.
    /// </summary>
    public Action<Entity, HiddenEventTarget, int, int>? Route { get; set; }

    public void Count(Entity player, HiddenEventTarget target, int what, int amount)
    {
        Route?.Invoke(player, target, what, amount);
    }
}
