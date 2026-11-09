using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.Quests.Enums;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The codes the client is told when a request about quests is refused.
/// </summary>
public static class QuestErrors
{
    /// <summary>
    /// The reward or the items of a condition did not fit.
    /// </summary>
    public const ushort ItemsNotGiven = 52010;

    public const ushort AlreadyTaken = 54001;
    public const ushort AlreadyFinished = 54002;

    /// <summary>
    /// The episode can not be finished now.
    /// </summary>
    public const ushort CannotFinish = 54003;

    /// <summary>
    /// The episode is not there: the person has not taken it, or the tables do not have it.
    /// </summary>
    public const ushort UnknownEpisode = 54004;

    public const ushort CannotGiveUp = 54005;
    public const ushort NoRoomInHelper = 54006;
    public const ushort LevelTooLow = 54018;
    public const ushort WrongHero = 54019;

    /// <summary>
    /// A repeated episode has been finished as often as a day allows. The game answers with the code of a wrong hero.
    /// </summary>
    public const ushort DoneForToday = WrongHero;
    public const ushort NotAvailableYet = 54020;
    public const ushort ObjectsNotGiven = 54021;
    public const ushort TooManyEpisodes = 54024;
}

/// <summary>
/// One condition of an episode that a person's act has moved on.
/// </summary>
/// <param name="Before">How many times the condition had been done before the act.</param>
public readonly record struct QuestStep(
    QuestEpisodeState State,
    QuestDefinition Quest,
    int Slot,
    QuestConditionDefinition Condition,
    byte Before,
    bool Completed)
{
    /// <summary>
    /// How many times the condition has been done now.
    /// </summary>
    public byte Value => State.Steps[Slot];

    /// <summary>
    /// Takes the act back, which is what happens when the items of a condition can not be given.
    /// </summary>
    public void Undo()
    {
        State.Steps[Slot] = Before;
        State.Done &= (short)~(1 << Slot);
    }
}

/// <summary>
/// What finishing an episode gives, ready to hand over.
/// </summary>
public sealed record QuestPayout(
    Reward Reward,
    IReadOnlyList<QuestItem> Items,
    int Prefix,
    int Suffix,
    int Helper);

/// <summary>
/// The rules of quests that do not need the world: who may take an episode, which acts move which conditions,
/// and what an episode gives.
/// </summary>
public static class QuestRules
{
    /// <summary>
    /// How many episodes a person may have at once.
    /// </summary>
    public const int MaxEpisodes = 30;

    /// <summary>
    /// How many episodes may show in the helper of the person.
    /// </summary>
    public const int MaxHelperEpisodes = 7;

    /// <summary>
    /// The error that refuses taking the episode, or null if the person may take it. A forced take, which scripts
    /// make, does not look at how many episodes there are and at the episodes that have to come first.
    /// </summary>
    public static ushort? CheckAccept(QuestCatalog catalog, QuestBook book, QuestDefinition quest, int level, int hero, bool force)
    {
        if (!force && book.Episodes.Count >= MaxEpisodes)
        {
            return QuestErrors.TooManyEpisodes;
        }

        if (book.Find(quest.Id) is not null)
        {
            return QuestErrors.AlreadyTaken;
        }

        if (IsFinished(book, quest))
        {
            return QuestErrors.AlreadyFinished;
        }

        if (quest.Level > level)
        {
            return QuestErrors.LevelTooLow;
        }

        if (quest.Hero != 0 && quest.Hero != hero)
        {
            return QuestErrors.WrongHero;
        }

        if (!force && quest.Before.Any(e => !catalog.TryGetEpisode(e, out var before) || !IsFinished(book, before)))
        {
            return QuestErrors.NotAvailableYet;
        }

        return quest.Conditions.Count == 0 ? QuestErrors.UnknownEpisode : null;
    }

    /// <summary>
    /// The time of the morning a game day starts at, when repeated episodes may be finished again.
    /// </summary>
    public static readonly TimeSpan RepeatResetTime = TimeSpan.FromHours(9);

    /// <summary>
    /// The error that refuses taking a repeated episode the person has finished as often as a day allows, or null.
    /// <paramref name="day"/> is the start of the game day that is running.
    /// </summary>
    public static ushort? CheckRepeat(QuestBook book, QuestDefinition quest, DateTimeOffset day)
    {
        return quest.Contents == QuestContents.Repeat &&
               book.Repeats.Find(e => e.Episode == quest.Id) is { } repeat &&
               repeat.Day >= day &&
               repeat.Count >= quest.RepeatsPerDay
            ? QuestErrors.DoneForToday
            : null;
    }

    /// <summary>
    /// Counts a repeated episode the person has just finished on the game day that starts at <paramref name="day"/>. A
    /// count of an earlier day starts over.
    /// </summary>
    public static void CountRepeat(QuestBook book, QuestDefinition quest, DateTimeOffset day)
    {
        if (quest.Contents != QuestContents.Repeat)
        {
            return;
        }

        if (book.Repeats.Find(e => e.Episode == quest.Id) is not { } repeat)
        {
            repeat = new QuestRepeatState { Episode = quest.Id };
            book.Repeats.Add(repeat);
        }

        if (repeat.Day < day)
        {
            repeat.Day = day;
            repeat.Count = 0;
        }

        repeat.Count++;
    }

    /// <summary>
    /// Whether the person has finished the episode. Repeated episodes are never finished for good.
    /// </summary>
    public static bool IsFinished(QuestBook book, QuestDefinition quest)
    {
        return quest.Contents != QuestContents.Repeat && book.IsFinished(quest.CompleteBit);
    }

    /// <summary>
    /// Whether an episode that starts by itself is due for the person: the episode before it is finished, it is neither
    /// finished nor in progress, and a main episode waits until no other main episode is in progress.
    /// </summary>
    public static bool ShouldStartByItself(QuestBook book, QuestCatalog catalog, QuestDefinition quest)
    {
        return quest.StartsByItself &&
               quest.Before.Count > 0 &&
               catalog.TryGetEpisode(quest.Before[0], out var before) &&
               IsFinished(book, before) &&
               !IsFinished(book, quest) &&
               book.Find(quest.Id) is null &&
               (quest.Contents != QuestContents.Main ||
                !book.Episodes.Any(e => catalog.TryGetEpisode(e.Episode, out var other) && other.Contents == QuestContents.Main));
    }

    /// <summary>
    /// Marks as done the conditions that were counted to their end but not marked, which a book stored before the end
    /// was handed over can hold. True when one was marked.
    /// </summary>
    public static bool MarkCountedConditions(QuestBook book, QuestCatalog catalog)
    {
        var marked = false;

        foreach (var state in book.Episodes)
        {
            if (!catalog.TryGetEpisode(state.Episode, out var quest))
            {
                continue;
            }

            for (var slot = 0; slot < quest.Conditions.Count; slot++)
            {
                if (!state.IsDone(slot) &&
                    catalog.TryGetCondition(quest.Conditions[slot], out var condition) &&
                    state.Steps[slot] >= condition.Counter)
                {
                    state.Done |= (short)(1 << slot);
                    marked = true;
                }
            }
        }

        return marked;
    }

    /// <summary>
    /// The state of an episode that has just been taken.
    /// </summary>
    public static QuestEpisodeState Start(QuestDefinition quest, bool helper)
    {
        return new QuestEpisodeState { Episode = quest.Id, Helper = helper };
    }

    /// <summary>
    /// Whether every condition of the episode is done, which is when it can be finished.
    /// </summary>
    public static bool IsReady(QuestDefinition quest, QuestEpisodeState state)
    {
        return quest.Conditions.Count > 0 && (state.Done & quest.AllDone) == quest.AllDone;
    }

    /// <summary>
    /// Whether an act with this parameter moves the condition. Most conditions name the thing they are about; the ones
    /// that are about buying, making and the like accept any thing when they name none.
    /// </summary>
    public static bool AcceptsParameter(QuestConditionDefinition condition, int parameter)
    {
        return condition.Type switch
        {
            QuestConditionType.Trigger or
                QuestConditionType.Buy or
                QuestConditionType.Sell or
                QuestConditionType.Make or
                QuestConditionType.Upgrade or
                QuestConditionType.Disassemble or
                QuestConditionType.Cultivation or
                QuestConditionType.Harvest => condition.TargetId == 0 || condition.TargetId == parameter,
            _ => condition.TargetId == parameter
        };
    }

    /// <summary>
    /// Whether the condition is about this kind of act done to this kind of thing, and the thing is the one it names.
    /// </summary>
    public static bool Matches(QuestConditionDefinition condition, QuestConditionType type, QuestConditionTarget target, int id)
    {
        return condition.Type == type && condition.Target == target && AcceptsParameter(condition, id);
    }

    /// <summary>
    /// Whether the condition can be moved on now: the one that has to be done first is, and it is not done itself.
    /// </summary>
    public static bool IsOpen(QuestDefinition quest, QuestEpisodeState state, int slot, QuestConditionDefinition condition)
    {
        if (state.Failed || state.IsDone(slot) || state.Steps[slot] >= condition.Counter)
        {
            return false;
        }

        if (condition.Previous == 0)
        {
            return true;
        }

        for (var i = 0; i < quest.Conditions.Count; i++)
        {
            if (quest.Conditions[i] == condition.Previous)
            {
                return state.IsDone(i);
            }
        }

        // The condition that has to come first is not part of the episode, so it can not be done.
        return false;
    }

    /// <summary>
    /// Counts an act. Every open condition of the book that <paramref name="matches"/> is moved on by
    /// <paramref name="count"/>, and the ones that reach their counter are marked as done.
    /// </summary>
    public static List<QuestStep> Apply(QuestBook book, QuestCatalog catalog, Func<QuestConditionDefinition, bool> matches, int count)
    {
        var steps = new List<QuestStep>();

        foreach (var state in book.Episodes)
        {
            if (!catalog.TryGetEpisode(state.Episode, out var quest))
            {
                continue;
            }

            for (var slot = 0; slot < quest.Conditions.Count; slot++)
            {
                if (!catalog.TryGetCondition(quest.Conditions[slot], out var condition) ||
                    !IsOpen(quest, state, slot, condition) ||
                    !matches(condition))
                {
                    continue;
                }

                var before = state.Steps[slot];
                var total = before + Math.Max(count, 0);
                state.Steps[slot] = (byte)Math.Min(total, condition.Counter);

                var completed = state.Steps[slot] >= condition.Counter;

                if (completed)
                {
                    state.Done |= (short)(1 << slot);
                }

                steps.Add(new QuestStep(state, quest, slot, condition, before, completed));
            }
        }

        return steps;
    }

    /// <summary>
    /// What an episode gives to a hero. <paramref name="selected"/> is the item the person has chosen from the ones to
    /// choose from, as it stands in the table.
    /// </summary>
    public static QuestPayout GetPayout(QuestDefinition quest, QuestCatalog catalog, int hero, int selected)
    {
        var reward = quest.Reward;
        var items = new List<QuestItem>();

        foreach (var entry in reward.Items)
        {
            AddItem(items, entry, reward.ItemsByClass, catalog, hero);
        }

        if (selected != 0)
        {
            foreach (var entry in reward.Selectable.Where(e => e.Item == selected))
            {
                AddItem(items, entry, reward.SelectByClass, catalog, hero);
            }
        }

        // A title of the table is a group that has a title for each class.
        return new QuestPayout(
            new Reward(items.Select(e => new RewardItem(e.Item, e.Count)).ToArray(), reward.Gold, reward.Ether, reward.BattlePoints, reward.Experience),
            items,
            catalog.GetClassTitle(reward.Prefix, hero),
            catalog.GetClassTitle(reward.Suffix, hero),
            reward.Helper);
    }

    private static void AddItem(List<QuestItem> items, QuestItem entry, bool byClass, QuestCatalog catalog, int hero)
    {
        var item = byClass ? catalog.GetClassReward(entry.Item, hero) : entry.Item;

        if (item != 0)
        {
            items.Add(new QuestItem(item, entry.Count));
        }
    }

    /// <summary>
    /// The bits the client takes as done conditions of an episode.
    /// </summary>
    public static short GetClientBits(QuestDefinition quest, QuestEpisodeState state) => (short)(state.Done & quest.AllDone);
}
