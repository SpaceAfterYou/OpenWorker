using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// What quests need to know about the items of a person and to take from them. Giving items goes through the
/// reward granter. The inventory is not part of this module: whoever owns it replaces <see cref="NoQuestItems"/>.
/// </summary>
public interface IQuestItems
{
    /// <summary>
    /// How many of an item the person has.
    /// </summary>
    ValueTask<int> CountAsync(Entity player, int item, CancellationToken cancellationToken);

    /// <summary>
    /// Takes the items from the person, as many of each as the person has up to the count.
    /// </summary>
    ValueTask TakeAsync(Entity player, IReadOnlyList<QuestItem> items, CancellationToken cancellationToken);
}

/// <summary>
/// Used while persons have no inventory: nobody has any items, so there is nothing to take.
/// </summary>
public sealed class NoQuestItems : IQuestItems
{
    public ValueTask<int> CountAsync(Entity player, int item, CancellationToken cancellationToken) => ValueTask.FromResult(0);

    public ValueTask TakeAsync(Entity player, IReadOnlyList<QuestItem> items, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
