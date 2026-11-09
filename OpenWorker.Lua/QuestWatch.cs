using Arch.Core;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Lua.Enums;

namespace OpenWorker.Lua;

/// <summary>
/// What happened to the quests of the player since the last look: the episodes taken, the conditions done and the
/// episodes finished, for a script that follows them (<c>OnUpdateQuest</c>). It compares the quests of the player with
/// what it saw before, so it hears of a change from any part of the game, a step of the clock later at most.
/// </summary>
public sealed class QuestWatch(World world, Entity player, QuestCatalog catalog)
{
    /// <summary>
    /// The episodes in progress and the conditions of each that were done at the last look.
    /// </summary>
    private readonly Dictionary<int, short> _seen = [];

    private readonly Dictionary<int, short> _now = [];
    private readonly List<(QuestUpdateType, int)> _changes = [];
    private readonly List<(QuestUpdateType, int)> _finished = [];

    private bool _ready;

    /// <summary>
    /// The changes since the last look, the episodes taken first, then the conditions, then the episodes finished. The
    /// first look only remembers what the player has. The list is the watch's own and is good until the next look.
    /// </summary>
    public IReadOnlyList<(QuestUpdateType Type, int Identifier)> Look()
    {
        var changes = _changes;
        var finished = _finished;
        var now = _now;

        changes.Clear();
        finished.Clear();
        now.Clear();

        if (!world.IsAlive(player) || !world.TryGet<QuestComponent>(player, out var quests) || quests.Book is not { } book)
        {
            return changes;
        }

        foreach (var state in book.Episodes)
        {
            now[state.Episode] = state.Done;
        }

        if (!_ready)
        {
            _ready = true;

            Remember(now);

            return changes;
        }

        foreach (var (episode, done) in now)
        {
            var before = _seen.GetValueOrDefault(episode);

            if (!_seen.ContainsKey(episode))
            {
                changes.Add((QuestUpdateType.AcceptQuest, episode));
            }

            AddConditions(changes, episode, (short)(done & ~before));
        }

        foreach (var (episode, done) in _seen)
        {
            if (now.ContainsKey(episode) || !catalog.TryGetEpisode(episode, out var quest) || !book.IsFinished(quest.CompleteBit))
            {
                continue;
            }

            // An episode that ended with its last condition is gone by the next look: its last conditions are told too.
            AddConditions(finished, episode, (short)(quest.AllDone & ~done));

            finished.Add((QuestUpdateType.CompleteQuest, episode));
        }

        changes.AddRange(finished);

        Remember(now);

        return changes;
    }

    private void Remember(Dictionary<int, short> now)
    {
        _seen.Clear();

        foreach (var (episode, done) in now)
        {
            _seen[episode] = done;
        }
    }

    private void AddConditions(List<(QuestUpdateType, int)> changes, int episode, short newlyDone)
    {
        if (newlyDone == 0 || !catalog.TryGetEpisode(episode, out var quest))
        {
            return;
        }

        for (var slot = 0; slot < quest.Conditions.Count; slot++)
        {
            if ((newlyDone & (1 << slot)) != 0)
            {
                changes.Add((QuestUpdateType.CompleteCondition, quest.Conditions[slot]));
            }
        }
    }
}
