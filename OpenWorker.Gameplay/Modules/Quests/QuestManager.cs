using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The quests of a person: which episodes the person has taken, how far each condition has come and which
/// episodes are finished. The state is kept in the database and loaded when the person enters a server.
/// </summary>
/// <remarks>
/// The rest of the game tells the manager what the person does: <see cref="OnMonsterKilledAsync"/>,
/// <see cref="OnMazeClearedAsync"/>, <see cref="OnSectorClearedAsync"/>, <see cref="OnItemCollectedAsync"/> and
/// <see cref="OnObjectUsedAsync"/>. The client tells what only it sees, such as talking to a character.
/// </remarks>
public sealed partial class QuestManager(
    World world,
    IQuestStore store,
    QuestCatalog catalog,
    IRewardGranter rewards,
    IQuestItems items,
    HelperManager helpers,
    BoosterManager boosters,
    ILogger<QuestManager> logger)
{
    /// <summary>
    /// The conditions the client may move on by itself because the server can not see them. The rest are counted
    /// from what the server sees, so the client can not make them up.
    /// </summary>
    private static readonly HashSet<QuestConditionType> ClientDriven =
    [
        QuestConditionType.Talk,
        QuestConditionType.Move,
        QuestConditionType.Trigger,
        QuestConditionType.Deliver,
        QuestConditionType.Protect,
        QuestConditionType.Survival,
        QuestConditionType.Trace,
        QuestConditionType.Guard
    ];

    #region Loading

    /// <summary>
    /// Loads the quests of the person and tells the client about them: the finished episodes first, then the ones
    /// in progress.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync(person, cancellationToken) ?? new QuestBook();

        book.Normalize(catalog);

        world.Set(player, new QuestComponent { Person = person, Book = book });

        Send(player, new QuestCompleteListResponse { CompletedEpisodeBits = book.Finished.ToArray() });

        SendQuestEpisodeList(player);
    }

    /// <summary>
    /// Tells the client the episodes the person has taken.
    /// </summary>
    public void SendQuestEpisodeList(Entity player)
    {
        if (TryGet(player, out var book, out _))
        {
            Send(player, BuildQuestListResponse(book));
        }

        else
        {
            Send(player, new QuestListResponse { List = [] });
        }
    }

    public QuestListResponse BuildQuestListResponse(Entity player)
    {
        return TryGet(player, out var book, out _) ? BuildQuestListResponse(book) : new QuestListResponse { List = [] };
    }

    private QuestListResponse BuildQuestListResponse(QuestBook book)
    {
        var list = new List<QuestEpisodeEntry>(book.Episodes.Count);

        foreach (var state in book.Episodes)
        {
            if (catalog.TryGetEpisode(state.Episode, out var quest))
            {
                list.Add(ToEntry(quest, state));
            }
        }

        return new QuestListResponse { List = list };
    }

    private static QuestEpisodeEntry ToEntry(QuestDefinition quest, QuestEpisodeState state)
    {
        var conditions = new QuestCondition[QuestModuleDefines.ConditionsPerEpisode];

        for (var slot = 0; slot < conditions.Length; slot++)
        {
            conditions[slot] = new QuestCondition
            {
                Condition = slot < quest.Conditions.Count ? quest.Conditions[slot] : 0,
                Step = state.Steps[slot]
            };
        }

        return new QuestEpisodeEntry
        {
            Episode = quest.Id,
            Info = new QuestInfoEntry
            {
                IsAddHelper = state.Helper,
                CompleteBit = QuestRules.GetClientBits(quest, state),
                IsFailed = state.Failed,
                Condition = conditions
            }
        };
    }

    #endregion Loading

    #region Taking, finishing and giving up

    /// <summary>
    /// The person asks to take an episode.
    /// </summary>
    public ValueTask<bool> AcceptAsync(Entity player, int episode, CancellationToken cancellationToken)
    {
        return AcceptCoreAsync(player, episode, false, cancellationToken);
    }

    /// <summary>
    /// A script gives the person an episode, without the checks of the episodes that have to come first.
    /// </summary>
    public void AcceptEpisode(Entity player, int episode)
    {
        Run(nameof(AcceptEpisode), () => AcceptCoreAsync(player, episode, true, CancellationToken.None).AsTask());
    }

    private async ValueTask<bool> AcceptCoreAsync(Entity player, int episode, bool force, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return false;
        }

        if (!catalog.TryGetEpisode(episode, out var quest))
        {
            SendError(player, QuestOpcode.Accept, QuestErrors.UnknownEpisode);
            return false;
        }

        var error = QuestRules.CheckAccept(catalog, book, quest, GetLevel(player), GetHero(player), force);

        if (error is { } code)
        {
            SendError(player, QuestOpcode.Accept, code);
            return false;
        }

        // The episode is taken before anything is waited for, so a second request for it is refused.
        var state = QuestRules.Start(quest, book.HelperCount < QuestRules.MaxHelperEpisodes);

        book.Episodes.Add(state);

        if (quest.GivenOnAccept.Count > 0 &&
            !await rewards.TryGrantAsync(player, new Reward(ToRewardItems(quest.GivenOnAccept)), RewardSource.Quest, cancellationToken))
        {
            book.Episodes.Remove(state);

            SendError(player, QuestOpcode.Accept, QuestErrors.ObjectsNotGiven);
            return false;
        }

        Send(player, new QuestAcceptResponse { Episode = quest.Id, IsShowHelper = state.Helper ? (byte)1 : (byte)0 });

        // Items the person already has count towards the conditions that ask for them.
        foreach (var condition in quest.Conditions.Select(e => catalog.TryGetCondition(e, out var found) ? found : null))
        {
            if (condition is { Type: QuestConditionType.Collect, Target: QuestConditionTarget.Item })
            {
                var count = await items.CountAsync(player, condition.TargetId, cancellationToken);

                if (count > 0)
                {
                    var id = condition.Id;

                    await ProgressAsync(player, e => e.Id == id, Math.Min(count, condition.Counter), false);
                }
            }
        }

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// The person asks to finish an episode and take its reward. <paramref name="selected"/> is the item chosen from
    /// the ones the episode offers, as it stands in the table.
    /// </summary>
    public async ValueTask<bool> CompleteAsync(Entity player, int episode, int selected, CancellationToken cancellationToken)
    {
        return await CompleteCoreAsync(player, episode, selected, false, cancellationToken);
    }

    /// <summary>
    /// A script finishes an episode of the person, whether or not its conditions are done.
    /// </summary>
    public void CompleteEpisode(Entity player, int episode)
    {
        Run(nameof(CompleteEpisode), () => CompleteCoreAsync(player, episode, 0, true, CancellationToken.None).AsTask());
    }

    private async ValueTask<bool> CompleteCoreAsync(Entity player, int episode, int selected, bool force, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return false;
        }

        var state = book.Find(episode);

        if (state is null || !catalog.TryGetEpisode(episode, out var quest) || !force && !QuestRules.IsReady(quest, state))
        {
            SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.CannotFinish);
            return false;
        }

        var payout = QuestRules.GetPayout(quest, catalog, GetHero(player), selected);

        // What the boosters of the person add is paid and told too.
        var experience = (long)boosters.Apply(player, BoosterEffect.CompleteQuestIncExp, payout.Reward.Experience);
        var reward = payout.Reward with { Experience = experience };

        // The episode is done before anything is waited for, so it can not be finished twice.
        var index = book.Episodes.IndexOf(state);
        var wasFinished = book.IsFinished(quest.CompleteBit);

        book.Episodes.RemoveAt(index);

        if (quest.Contents != QuestContents.Repeat)
        {
            book.SetFinished(quest.CompleteBit, true);
        }

        bool granted;

        try
        {
            granted = await rewards.TryGrantAsync(player, reward, RewardSource.Quest, cancellationToken);
        }

        catch
        {
            Restore(book, state, index, quest, wasFinished);
            throw;
        }

        if (!granted)
        {
            Restore(book, state, index, quest, wasFinished);

            SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.ItemsNotGiven);
            return false;
        }

        uint helper = 0;

        if (payout.Helper != 0 && await helpers.AddAsync(player, payout.Helper, cancellationToken))
        {
            helper = (uint)payout.Helper;
        }

        if (quest.TakenOnFinish.Count > 0)
        {
            await items.TakeAsync(player, quest.TakenOnFinish, cancellationToken);
        }

        Send(player, new QuestEpisodeCompleteResponse
        {
            Episode = quest.Id,
            Exp = (int)Math.Min(experience, int.MaxValue),
            Money = reward.Gold,
            BattlePoints = reward.BattlePoints,
            Ether = reward.Ether,
            Title = new TitleValue(payout.Prefix, payout.Suffix),
            NpcHelper = helper,
            Items = payout.Items.Select(e => new CreateItemValue { Item = e.Item, Count = e.Count }).ToArray()
        });

        await SaveAsync(player);

        return true;
    }

    private static void Restore(QuestBook book, QuestEpisodeState state, int index, QuestDefinition quest, bool wasFinished)
    {
        book.Episodes.Insert(Math.Min(index, book.Episodes.Count), state);

        if (!wasFinished)
        {
            book.SetFinished(quest.CompleteBit, false);
        }
    }

    /// <summary>
    /// The person gives an episode up.
    /// </summary>
    public async ValueTask<bool> GiveUpAsync(Entity player, int episode, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return false;
        }

        var state = book.Find(episode);

        if (state is null || !catalog.TryGetEpisode(episode, out var quest) || quest.CannotGiveUp)
        {
            SendError(player, QuestOpcode.GiveUp, QuestErrors.CannotGiveUp);
            return false;
        }

        book.Remove(episode);

        // What the episode gave with it goes back.
        if (quest.GivenOnAccept.Count > 0)
        {
            await items.TakeAsync(player, quest.GivenOnAccept, cancellationToken);
        }

        Send(player, new QuestGiveUpResponse { Episode = episode });

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// The episode has failed, which the game decides: the person left a maze while escorting someone, say.
    /// The episode stays on the list as failed until the client asks to remove it.
    /// </summary>
    public async ValueTask<bool> FailAsync(Entity player, int episode)
    {
        if (!TryGet(player, out var book, out var person) || book.Find(episode) is not { Failed: false } state)
        {
            return false;
        }

        state.Failed = true;

        Send(player, new QuestFailResponse { Episode = episode });

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// The client has seen an episode fail and asks to remove it. Nothing is answered.
    /// </summary>
    public async ValueTask<bool> RemoveFailedAsync(Entity player, int episode)
    {
        if (!TryGet(player, out var book, out var person) || book.Find(episode) is not { Failed: true })
        {
            return false;
        }

        book.Remove(episode);

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// The person asks to show an episode in the helper or to hide it.
    /// </summary>
    public async ValueTask<bool> SetHelperAsync(Entity player, QuestHelperType type, int episode)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return false;
        }

        if (book.Find(episode) is not { } state)
        {
            SendError(player, QuestOpcode.Helper, QuestErrors.UnknownEpisode);
            return false;
        }

        var add = type == QuestHelperType.Add;

        if (add && !state.Helper && book.HelperCount >= QuestRules.MaxHelperEpisodes)
        {
            SendError(player, QuestOpcode.Helper, QuestErrors.NoRoomInHelper);
            return false;
        }

        state.Helper = add;

        Send(player, new QuestHelperResponse { Type = type, Episode = episode });

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// Forgets one episode as if the person had never taken it or finished it, and tells the client.
    /// </summary>
    public async ValueTask<bool> ResetEpisodeAsync(Entity player, int episode)
    {
        if (!TryGet(player, out var book, out var person) || !catalog.TryGetEpisode(episode, out var quest))
        {
            return false;
        }

        book.Remove(episode);
        book.SetFinished(quest.CompleteBit, false);

        Send(player, new QuestEpisodeResetResponse { Episode = episode });

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// Forgets all the quests of the person, the finished ones too, and tells the client.
    /// </summary>
    public async ValueTask<bool> ResetAllAsync(Entity player)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return false;
        }

        book.Episodes.Clear();
        Array.Clear(book.Finished);

        Send(player, new QuestResetResponse());

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// A script clears all the quests of the person.
    /// </summary>
    public void RemoveAllActiveEpisodes(Entity player)
    {
        Run(nameof(RemoveAllActiveEpisodes), () => ResetAllAsync(player).AsTask());
    }

    #endregion Taking, finishing and giving up

    #region Progress

    /// <summary>
    /// The person has killed a monster: it counts for the conditions that name the monster, for the ones that name
    /// a group it belongs to and for each person in the maze, which is for the caller to tell.
    /// </summary>
    public ValueTask OnMonsterKilledAsync(Entity player, int monster, CancellationToken cancellationToken)
    {
        var groups = catalog.GetMonsterGroups(monster);

        return ProgressAsync(
            player,
            e => e.Type == QuestConditionType.Hunt &&
                 (e.Target == QuestConditionTarget.Monster && QuestRules.AcceptsParameter(e, monster) ||
                  e.Target == QuestConditionTarget.MonsterGroup && groups.Any(group => QuestRules.AcceptsParameter(e, group))),
            1);
    }

    /// <summary>
    /// The person has cleared a maze.
    /// </summary>
    public ValueTask OnMazeClearedAsync(Entity player, int maze, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Maze, maze), 1);
    }

    /// <summary>
    /// The person has cleared a sector of a maze.
    /// </summary>
    public ValueTask OnSectorClearedAsync(Entity player, int maze, int sector, CancellationToken cancellationToken)
    {
        return ProgressAsync(
            player,
            e => e.Maze == maze && QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Sector, sector),
            1);
    }

    /// <summary>
    /// The person has got items.
    /// </summary>
    public ValueTask OnItemCollectedAsync(Entity player, int item, short count, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => QuestRules.Matches(e, QuestConditionType.Collect, QuestConditionTarget.Item, item), count);
    }

    /// <summary>
    /// The person has used an object of a maze, such as a box to click.
    /// </summary>
    public ValueTask OnObjectUsedAsync(Entity player, int box, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => QuestRules.Matches(e, QuestConditionType.Trigger, QuestConditionTarget.WorldObject, box), 1);
    }

    /// <summary>
    /// Something has happened that the conditions name as an event, such as a hidden event of a maze.
    /// </summary>
    public ValueTask OnEventAsync(Entity player, int eventId, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Event, eventId), 1);
    }

    /// <summary>
    /// The person has done something with items that a condition names, such as buying or making them.
    /// </summary>
    public ValueTask OnItemUsedAsync(Entity player, QuestConditionType type, int item, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => QuestRules.Matches(e, type, QuestConditionTarget.Item, item), 1);
    }

    /// <summary>
    /// The person has stepped into a box of a map that belongs to a condition.
    /// </summary>
    public ValueTask OnConditionAdvancedAsync(Entity player, int condition, int count, CancellationToken cancellationToken)
    {
        return ProgressAsync(player, e => e.Id == condition, count);
    }

    /// <summary>
    /// The client says it has done a step of a condition, such as talking to a character.
    /// </summary>
    public ValueTask OnEventUpdateAsync(Entity player, int condition, long parameter, CancellationToken cancellationToken)
    {
        return ProgressAsync(
            player,
            e => e.Id == condition && ClientDriven.Contains(e.Type) && parameter is >= int.MinValue and <= int.MaxValue && QuestRules.AcceptsParameter(e, (int)parameter),
            1);
    }

    /// <summary>
    /// Sets a condition to a step, for tests and for game masters. A step the condition does not reach finishes it.
    /// </summary>
    public bool TryApplyConditionProgress(Entity player, int conditionId, byte step)
    {
        if (!TryGet(player, out var book, out _) || !catalog.TryGetCondition(conditionId, out var condition) || step > condition.Counter)
        {
            return false;
        }

        foreach (var state in book.Episodes)
        {
            if (catalog.TryGetEpisode(state.Episode, out var quest) && quest.Conditions.ToList().IndexOf(conditionId) is var slot and >= 0)
            {
                var missing = step - state.Steps[slot];

                if (missing > 0)
                {
                    Run(nameof(TryApplyConditionProgress), () => ProgressAsync(player, e => e.Id == conditionId, missing).AsTask());
                }

                return true;
            }
        }

        return false;
    }

    private async ValueTask ProgressAsync(Entity player, Func<QuestConditionDefinition, bool> matches, int count, bool save = true)
    {
        if (!TryGet(player, out var book, out var person))
        {
            return;
        }

        var steps = QuestRules.Apply(book, catalog, matches, count);

        if (steps.Count == 0)
        {
            return;
        }

        var kept = new List<QuestStep>(steps.Count);

        foreach (var step in steps)
        {
            // The items a condition gives when it is done must fit, or the condition is not done.
            if (step.Completed &&
                step.Condition.GivenOnDone.Count > 0 &&
                !await rewards.TryGrantAsync(player, new Reward(ToRewardItems(step.Condition.GivenOnDone)), RewardSource.Quest, CancellationToken.None))
            {
                step.Undo();

                SendError(player, QuestOpcode.Update, QuestErrors.ItemsNotGiven);
                continue;
            }

            kept.Add(step);
        }

        if (kept.Count == 0)
        {
            return;
        }

        Send(player, new QuestUpdateResponse
        {
            ConditionList = kept.Select(e => new QuestCondition { Condition = e.Condition.Id, Step = e.Value }).ToArray()
        });

        foreach (var step in kept.Where(e => e.Completed && e.Condition.TakenOnDone.Count > 0))
        {
            await items.TakeAsync(player, step.Condition.TakenOnDone, CancellationToken.None);
        }

        // An episode that ends by itself is finished as soon as its last condition is done.
        foreach (var quest in kept.Where(e => e.Completed && e.Quest.FinishesByItself && QuestRules.IsReady(e.Quest, e.State)).Select(e => e.Quest).DistinctBy(e => e.Id))
        {
            await CompleteCoreAsync(player, quest.Id, 0, false, CancellationToken.None);
        }

        if (save)
        {
            await SaveAsync(player);
        }
    }

    #endregion Progress

    #region Questions

    public bool HasAnyActiveEpisode(Entity player) => TryGet(player, out var book, out _) && book.Episodes.Count > 0;

    public bool HasActiveEpisode(Entity player, int episodeId) => TryGet(player, out var book, out _) && book.Find(episodeId) is not null;

    public bool ActiveEpisodeUsesCondition(Entity player, int conditionId)
    {
        return conditionId != 0 &&
               TryGet(player, out var book, out _) &&
               book.Episodes.Any(e => catalog.TryGetEpisode(e.Episode, out var quest) && quest.Conditions.Contains(conditionId));
    }

    /// <summary>
    /// Whether the person has finished the episode.
    /// </summary>
    public bool HasFinishedEpisode(Entity player, int episodeId)
    {
        return TryGet(player, out var book, out _) && catalog.TryGetEpisode(episodeId, out var quest) && QuestRules.IsFinished(book, quest);
    }

    #endregion Questions

    #region Helpers

    private int GetLevel(Entity player)
    {
        return world.IsAlive(player) && world.TryGet<LevelComponent>(player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel;
    }

    private int GetHero(Entity player)
    {
        return world.IsAlive(player) && world.TryGet<PersonInfoComponent>(player, out var info) ? (int)info.Hero : 0;
    }

    private static RewardItem[] ToRewardItems(IReadOnlyList<QuestItem> list)
    {
        return list.Select(e => new RewardItem(e.Item, e.Count)).ToArray();
    }

    private bool TryGet(Entity player, out QuestBook book, out int person)
    {
        if (world.IsAlive(player) && world.TryGet<QuestComponent>(player, out var component) && component.Book is not null)
        {
            book = component.Book;
            person = component.Person;
            return true;
        }

        book = null!;
        person = 0;
        return false;
    }

    private void Send(Entity player, IResponseHotspotMessage message)
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(message);
        }
    }

    private void SendError(Entity player, QuestOpcode command, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Quest, command), code));
    }

    /// <summary>
    /// The quests of a player as the flush writes them (<see cref="PersonSections.Quests"/>).
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<QuestComponent>(PersonStateKind.Quest, PersonSections.Quests, e => e.Book);

    /// <summary>
    /// Marks the book as changed: the flush of the person writes it, nothing here waits for the database. The change is
    /// already told to the client.
    /// </summary>
    private ValueTask SaveAsync(Entity player)
    {
        world.MarkDirty(player, PersonSections.Quests);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Runs work that the caller can not wait for, such as a script. It continues on the world loop and its
    /// failure is logged.
    /// </summary>
    private void Run(string what, Func<Task> work)
    {
        _ = RunAsync(what, work);
    }

    private async Task RunAsync(string what, Func<Task> work)
    {
        try
        {
            await work();
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogWorkFailed(logger, e, what);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Work} of the quests failed.")]
    private static partial void LogWorkFailed(ILogger logger, Exception exception, string work);

    #endregion Helpers
}
