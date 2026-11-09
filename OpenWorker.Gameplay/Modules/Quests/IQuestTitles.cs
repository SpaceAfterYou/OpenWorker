using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// Gives the titles an episode rewards. The titles of a person are kept by a layer above the quests, which replaces
/// <see cref="NoQuestTitles"/>.
/// </summary>
public interface IQuestTitles
{
    /// <summary>
    /// Gives the person the title if the person can use it and does not have it yet.
    /// </summary>
    ValueTask GrantAsync(Entity player, int title);
}

/// <summary>
/// Used where nothing keeps titles: the title is told with the reward and not kept.
/// </summary>
public sealed class NoQuestTitles : IQuestTitles
{
    public ValueTask GrantAsync(Entity player, int title) => ValueTask.CompletedTask;
}
