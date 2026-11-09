using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// Where the quests of a person are read from when it enters. They are written by the flush of the person
/// (<see cref="QuestManager.Document"/>).
/// </summary>
public interface IQuestStore
{
    /// <summary>
    /// The stored quests, or null if the person has none yet.
    /// </summary>
    ValueTask<QuestBook?> LoadAsync(int person, CancellationToken cancellationToken);
}

/// <summary>
/// Keeps the quests in the progress documents of the person, next to what the other features keep.
/// </summary>
public sealed class PersonStateQuestStore(PersonStateStore store) : IQuestStore
{
    public ValueTask<QuestBook?> LoadAsync(int person, CancellationToken cancellationToken)
    {
        return store.LoadAsync<QuestBook>(person, PersonStateKind.Quest, cancellationToken);
    }
}
