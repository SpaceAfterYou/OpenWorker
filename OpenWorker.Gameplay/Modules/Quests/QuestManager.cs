using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.Quests.Responses;
using OpenWorker.Hotspot.Modules.Quests.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The quests of a person: which episodes the person has taken, how far each condition has come and which
/// episodes are finished. The state is kept in the database and loaded when the person enters a server.
/// </summary>
/// <remarks>
/// The rest of the game tells the manager what the person does: <see cref="OnMonsterKilledAsync"/>,
/// <see cref="OnMazeClearedAsync"/>, <see cref="OnSectorClearedAsync"/>, <see cref="OnItemsArrived"/>,
/// <see cref="OnObjectUsedAsync"/> and <see cref="OnEvent"/> for the work on items. The client tells what only it sees,
/// talking to a character, and the server checks what it can of it.
/// </remarks>
public sealed partial class QuestManager(
    World world,
    IQuestStore store,
    QuestCatalog catalog,
    IRewardGranter rewards,
    IQuestItems items,
    HelperManager helpers,
    BoosterManager boosters,
    ILogger<QuestManager> logger,
    AchievementManager? achievements = null,
    ExperienceService? experience = null,
    IQuestNpcs? npcs = null,
    IQuestTitles? titles = null,
    TimeProvider? time = null,
    ReadOnlyCollection<QuestConditionRuleRow>? rules = null)
{
    /// <summary>
    /// The conditions that count what one person does for everyone in the maze: the items one gets count for the others.
    /// </summary>
    private readonly HashSet<int> _countedForGroup = (rules ?? new ReadOnlyCollection<QuestConditionRuleRow>([]))
        .Where(e => e.CountsForGroup)
        .Select(e => (int)e.Id)
        .ToHashSet();

    /// <summary>
    /// The conditions the client tells the server about (<c>15/08</c>): talking to a character, and handing something in to
    /// one, which the tables write as a talk that takes items. Everything else is counted from what the server sees.
    /// </summary>
    private static readonly HashSet<QuestConditionType> ToldByTheClient =
    [
        QuestConditionType.Talk,
        QuestConditionType.Deliver
    ];

    private readonly IQuestNpcs _npcs = npcs ?? new NoQuestNpcs();
    private readonly IQuestTitles _titles = titles ?? new NoQuestTitles();
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <summary>
    /// How an episode comes to the person, which decides the checks it goes through.
    /// </summary>
    private enum Taking
    {
        /// <summary>The client asks at a character: every check.</summary>
        Asked,

        /// <summary>It starts by itself after the episode before it: no limit of episodes, no character.</summary>
        ByItself,

        /// <summary>A script gives it: only what the person can not have (an episode taken or finished, a level, a hero).</summary>
        Given
    }

    #region Loading

    /// <summary>
    /// Loads the quests of the person and tells the client about them: the episodes in progress, then the finished ones,
    /// which is the order the client reads them in to know which episodes it may offer.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync(person, cancellationToken) ?? new QuestBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        book.Normalize(catalog);

        world.Set(player, new QuestComponent { Person = person, Book = book });

        var changed = QuestRules.MarkCountedConditions(book, catalog);

        // Episodes that start by themselves once the one before them is finished, and that the person lacks: a finish that
        // came from another server or a script, or a book from before they started by themselves. The lists that follow
        // carry them.
        foreach (var quest in catalog.StartingByThemselves)
        {
            if (QuestRules.ShouldStartByItself(book, catalog, quest))
            {
                changed |= await AcceptCoreAsync(player, quest.Id, Taking.ByItself, false, cancellationToken);
            }
        }

        if (!TryGet(player, out book, out _))
        {
            return;
        }

        Send(player, BuildQuestListResponse(book));
        Send(player, new QuestCompleteListResponse { CompletedEpisodeBits = book.Finished.ToArray() });

        if (changed)
        {
            await SaveAsync(player);
        }
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
    /// The person asks to take an episode, at the character that gives it.
    /// </summary>
    public async ValueTask<bool> AcceptAsync(Entity player, int episode, CancellationToken cancellationToken)
    {
        if (await AcceptCoreAsync(player, episode, Taking.Asked, true, cancellationToken))
        {
            await SaveAsync(player);
            return true;
        }

        return false;
    }

    /// <summary>
    /// A script gives the person an episode, without the checks of the episodes that have to come first.
    /// </summary>
    public void AcceptEpisode(Entity player, int episode)
    {
        Run(nameof(AcceptEpisode), async () =>
        {
            if (await AcceptCoreAsync(player, episode, Taking.Given, true, CancellationToken.None))
            {
                await SaveAsync(player);
            }
        });
    }

    /// <summary>
    /// Takes an episode: the book has it in progress afterwards, with the items it gives and its conditions counted for
    /// the items the person already has. <paramref name="tell"/> sends the client the episode; the lists of an entry carry
    /// it instead.
    /// </summary>
    private async ValueTask<bool> AcceptCoreAsync(Entity player, int episode, Taking taking, bool tell, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out _))
        {
            return false;
        }

        if (!catalog.TryGetEpisode(episode, out var quest))
        {
            Refuse(player, QuestOpcode.Accept, QuestErrors.UnknownEpisode, tell);
            return false;
        }

        var error = QuestRules.CheckAccept(catalog, book, quest, GetLevel(player), GetHero(player), taking == Taking.Given) ??
                    (taking == Taking.Given ? null : QuestRules.CheckRepeat(book, quest, Today()));

        if (error is null && taking == Taking.Asked && quest.StartNpc != 0 && _npcs.Reach(player, quest.StartNpc) == NpcReach.Far)
        {
            LogTooFar(logger, nameof(AcceptAsync), episode, quest.StartNpc);
            error = QuestErrors.NotAvailableYet;
        }

        if (error is { } code)
        {
            Refuse(player, QuestOpcode.Accept, code, tell);
            return false;
        }

        // What the episode gives comes first and all of it, or the episode is not taken. The episode is not in the book
        // while it is given, so the items do not count for its conditions twice; they are counted below.
        if (quest.GivenOnAccept.Count > 0 &&
            !await rewards.TryGrantAsync(player, new Reward(ToRewardItems(quest.GivenOnAccept)), RewardSource.Quest, cancellationToken))
        {
            Refuse(player, QuestOpcode.Accept, QuestErrors.ObjectsNotGiven, tell);
            return false;
        }

        // Another request may have taken it while the items were given.
        if (!TryGet(player, out book, out _) || book.Find(quest.Id) is not null)
        {
            return false;
        }

        var state = QuestRules.Start(quest, book.HelperCount < QuestRules.MaxHelperEpisodes);

        book.Episodes.Add(state);

        if (tell)
        {
            Send(player, new QuestAcceptResponse { Episode = quest.Id, IsShowHelper = state.Helper ? (byte)1 : (byte)0 });
        }

        // Items the person already has count towards the conditions that ask for them, as many as the person has.
        await RecountHeldAsync(player, cancellationToken);

        return true;
    }

    /// <summary>
    /// Counts the items the person holds again for the collecting conditions that are open, which is the one time an item that
    /// has gone since it came is taken off the count: the condition is set to the items held (at most its counter), up or
    /// down. A person who holds none of the item is left as it stands. The counting is done when an episode is taken and
    /// whenever another condition of a person is done; otherwise a condition only counts items as they arrive
    /// (<see cref="OnItemsArrived"/>).
    /// </summary>
    private async ValueTask RecountHeldAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out _))
        {
            return;
        }

        var collecting = new List<(int Episode, int Slot, QuestConditionDefinition Condition)>();

        foreach (var state in book.Episodes)
        {
            if (!catalog.TryGetEpisode(state.Episode, out var quest))
            {
                continue;
            }

            for (var slot = 0; slot < quest.Conditions.Count; slot++)
            {
                if (catalog.TryGetCondition(quest.Conditions[slot], out var condition) &&
                    condition is { Type: QuestConditionType.Collect, Target: QuestConditionTarget.Item } &&
                    QuestRules.IsOpen(quest, state, slot, condition))
                {
                    collecting.Add((state.Episode, slot, condition));
                }
            }
        }

        var lowered = new List<QuestCondition>();

        foreach (var (episode, slot, condition) in collecting)
        {
            var held = Math.Min(await items.CountAsync(player, condition.TargetId, cancellationToken), condition.Counter);

            // The person may have gone, or the episode, or the condition may have moved on while the items were counted.
            if (held <= 0 || !TryGet(player, out book, out _) || book.Find(episode) is not { } current ||
                !catalog.TryGetEpisode(episode, out var quest) || !QuestRules.IsOpen(quest, current, slot, condition))
            {
                continue;
            }

            var counted = current.Steps[slot];

            if (held > counted)
            {
                var id = condition.Id;

                await ProgressAsync(player, e => e.Id == id, held - counted, false, recount: false);
            }
            else if (held < counted)
            {
                current.Steps[slot] = (byte)held;

                lowered.Add(new QuestCondition { Condition = condition.Id, Step = (byte)held });
            }
        }

        if (lowered.Count > 0)
        {
            Send(player, new QuestUpdateResponse { ConditionList = lowered.ToArray() });
        }
    }

    /// <summary>
    /// The person asks to finish an episode and take its reward, at the character it is handed in to.
    /// <paramref name="selected"/> is the item chosen from the ones the episode offers, as it stands in the table.
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
        if (!TryGet(player, out var book, out _))
        {
            return false;
        }

        var state = book.Find(episode);

        if (state is null || !catalog.TryGetEpisode(episode, out var quest) || !force && !QuestRules.IsReady(quest, state))
        {
            SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.CannotFinish);
            return false;
        }

        // An episode handed in to a character is handed in where the character stands; one that ends by itself is not.
        if (!force && !quest.FinishesByItself && quest.FinishNpc != 0 && _npcs.Reach(player, quest.FinishNpc) == NpcReach.Far)
        {
            LogTooFar(logger, nameof(CompleteAsync), episode, quest.FinishNpc);
            SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.CannotFinish);
            return false;
        }

        // What the episode takes at its end must still be there: items counted when they came and sold or given away since
        // do not finish it. A script that ends an episode takes what is left.
        if (!force)
        {
            foreach (var (item, count) in quest.TakenOnFinish)
            {
                if (await items.CountAsync(player, item, cancellationToken) < count)
                {
                    SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.CannotFinish);
                    return false;
                }
            }

            if (!TryGet(player, out book, out _) || book.Find(episode) != state)
            {
                return false;
            }
        }

        var payout = QuestRules.GetPayout(quest, catalog, GetHero(player), selected);

        // What the boosters of the person add is paid and told too.
        var earned = (long)boosters.Apply(player, BoosterEffect.CompleteQuestIncExp, payout.Reward.Experience);
        var bonus = Math.Max(0, earned - payout.Reward.Experience);
        var reward = payout.Reward with { Experience = earned };

        // The episode is done before anything is waited for, so it can not be finished twice.
        var index = book.Episodes.IndexOf(state);
        var wasFinished = book.IsFinished(quest.CompleteBit);
        var repeats = book.Repeats.Find(e => e.Episode == quest.Id) is { } repeat ? (repeat.Day, repeat.Count) : ((DateTimeOffset, int)?)null;

        book.Episodes.RemoveAt(index);

        if (quest.Contents != QuestContents.Repeat)
        {
            book.SetFinished(quest.CompleteBit, true);
        }

        QuestRules.CountRepeat(book, quest, Today());

        bool granted;

        try
        {
            granted = await rewards.TryGrantAsync(player, reward, RewardSource.Quest, cancellationToken);
        }

        catch
        {
            Restore(book, state, index, quest, wasFinished, repeats);
            throw;
        }

        if (!granted)
        {
            Restore(book, state, index, quest, wasFinished, repeats);

            SendError(player, QuestOpcode.EpisodeComplete, QuestErrors.ItemsNotGiven);
            return false;
        }

        if (quest.TakenOnFinish.Count > 0)
        {
            await items.TakeAsync(player, quest.TakenOnFinish, cancellationToken);
        }

        if (!world.IsAlive(player))
        {
            return false;
        }

        // Experience raises the level; the reward granter only hands over items and money.
        experience?.Add(player, (int)Math.Min(earned, int.MaxValue), (int)Math.Min(bonus, int.MaxValue));

        foreach (var title in new[] { payout.Prefix, payout.Suffix }.Where(e => e != 0))
        {
            await _titles.GrantAsync(player, title);

            if (!world.IsAlive(player))
            {
                return false;
            }
        }

        uint helper = 0;

        if (payout.Helper != 0 && await helpers.AddAsync(player, payout.Helper, cancellationToken))
        {
            helper = (uint)payout.Helper;
        }

        Send(player, new QuestEpisodeCompleteResponse
        {
            Episode = quest.Id,
            Exp = (int)Math.Min(earned, int.MaxValue),
            Money = reward.Gold,
            BattlePoints = reward.BattlePoints,
            Ether = reward.Ether,
            Title = new TitleValue(payout.Prefix, payout.Suffix),
            NpcHelper = helper,
            Items = payout.Items.Select(e => new CreateItemValue { Item = e.Item, Count = e.Count }).ToArray()
        });

        achievements?.OnQuestFinished(player, quest.Contents);

        // The episodes that follow and start by themselves are taken at once.
        foreach (var link in quest.Links)
        {
            if (catalog.TryGetEpisode(link, out var next) && next.StartsByItself)
            {
                await AcceptCoreAsync(player, link, Taking.ByItself, true, cancellationToken);
            }
        }

        await SaveAsync(player);

        return true;
    }

    private static void Restore(
        QuestBook book, QuestEpisodeState state, int index, QuestDefinition quest, bool wasFinished, (DateTimeOffset Day, int Count)? repeats)
    {
        book.Episodes.Insert(Math.Min(index, book.Episodes.Count), state);

        if (!wasFinished)
        {
            book.SetFinished(quest.CompleteBit, false);
        }

        book.Repeats.RemoveAll(e => e.Episode == quest.Id);

        if (repeats is { } before)
        {
            book.Repeats.Add(new QuestRepeatState { Episode = quest.Id, Day = before.Day, Count = before.Count });
        }
    }

    /// <summary>
    /// The person gives an episode up. What the episode gave goes back, and what its done conditions took is given back,
    /// which has to fit.
    /// </summary>
    public async ValueTask<bool> GiveUpAsync(Entity player, int episode, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book, out _))
        {
            return false;
        }

        var state = book.Find(episode);

        if (state is null || !catalog.TryGetEpisode(episode, out var quest) || quest.CannotGiveUp)
        {
            SendError(player, QuestOpcode.GiveUp, QuestErrors.CannotGiveUp);
            return false;
        }

        var done = Enumerable.Range(0, quest.Conditions.Count)
            .Where(state.IsDone)
            .Select(slot => catalog.TryGetCondition(quest.Conditions[slot], out var condition) ? condition : null)
            .OfType<QuestConditionDefinition>()
            .ToArray();

        var returned = done.SelectMany(e => e.TakenOnDone).ToArray();

        book.Remove(episode);

        if (returned.Length > 0 &&
            !await rewards.TryGrantAsync(player, new Reward(ToRewardItems(returned)), RewardSource.Quest, cancellationToken))
        {
            if (TryGet(player, out book, out _) && book.Find(episode) is null)
            {
                book.Episodes.Add(state);
            }

            SendError(player, QuestOpcode.GiveUp, QuestErrors.CannotGiveUp);
            return false;
        }

        // What the episode and its done conditions gave goes back.
        var given = quest.GivenOnAccept.Concat(done.SelectMany(e => e.GivenOnDone)).ToArray();

        if (given.Length > 0)
        {
            await items.TakeAsync(player, given, cancellationToken);
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
        if (!TryGet(player, out var book, out _) || book.Find(episode) is not { Failed: false } state)
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
        if (!TryGet(player, out var book, out _) || book.Find(episode) is not { Failed: true })
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
        if (!TryGet(player, out var book, out _))
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
        if (!TryGet(player, out var book, out _) || !catalog.TryGetEpisode(episode, out var quest))
        {
            return false;
        }

        book.Remove(episode);
        book.SetFinished(quest.CompleteBit, false);
        book.Repeats.RemoveAll(e => e.Episode == episode);

        Send(player, new QuestEpisodeResetResponse { Episode = episode });

        await SaveAsync(player);

        return true;
    }

    /// <summary>
    /// Forgets all the quests of the person, the finished ones too, and tells the client.
    /// </summary>
    public async ValueTask<bool> ResetAllAsync(Entity player)
    {
        if (!TryGet(player, out var book, out _))
        {
            return false;
        }

        book.Episodes.Clear();
        book.Repeats.Clear();
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

        return CountAsync(
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
        return CountAsync(player, e => QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Maze, maze), 1);
    }

    /// <summary>
    /// The person has cleared a sector of a maze.
    /// </summary>
    public ValueTask OnSectorClearedAsync(Entity player, int maze, int sector, CancellationToken cancellationToken)
    {
        return CountAsync(
            player,
            e => e.Maze == maze && QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Sector, sector),
            1);
    }

    /// <summary>
    /// The person has got items.
    /// </summary>
    public ValueTask OnItemCollectedAsync(Entity player, int item, short count, CancellationToken cancellationToken)
    {
        return CountAsync(player, e => QuestRules.Matches(e, QuestConditionType.Collect, QuestConditionTarget.Item, item), count);
    }

    /// <summary>
    /// Items came into the bags of the person, from wherever they came: they count for the conditions that ask to collect
    /// them. The inventory reports every gift here (<c>ItemArrivals</c>), so the work runs without the caller waiting.
    /// </summary>
    public void OnItemsArrived(Entity player, int item, int count)
    {
        if (count <= 0 || !TryGet(player, out _, out _))
        {
            return;
        }

        var added = (short)Math.Min(count, short.MaxValue);

        Run(nameof(OnItemsArrived), () => OnItemCollectedAsync(player, item, added, CancellationToken.None).AsTask());
    }

    /// <summary>
    /// Whether what one person does for the condition counts for everyone in the maze.
    /// </summary>
    public bool CountsForGroup(int conditionId) => _countedForGroup.Contains(conditionId);

    /// <summary>
    /// A person in the maze of the person got items, and the conditions that count for everyone in the maze take them too:
    /// whoever is in the maze with them counts the items, though they are not in their bags.
    /// </summary>
    public void OnItemsArrivedForGroup(Entity player, int item, int count)
    {
        if (count <= 0 || _countedForGroup.Count == 0 || !TryGet(player, out _, out _))
        {
            return;
        }

        var added = (short)Math.Min(count, short.MaxValue);

        Run(nameof(OnItemsArrivedForGroup), () => CountAsync(player, e => _countedForGroup.Contains(e.Id) && QuestRules.Matches(e, QuestConditionType.Collect, QuestConditionTarget.Item, item), added).AsTask());
    }

    /// <summary>
    /// The person has used an object of a maze, such as a box to click.
    /// </summary>
    public ValueTask OnObjectUsedAsync(Entity player, int box, CancellationToken cancellationToken)
    {
        return CountAsync(player, e => QuestRules.Matches(e, QuestConditionType.Trigger, QuestConditionTarget.WorldObject, box), 1);
    }

    /// <summary>
    /// Something has happened that the conditions name as an event, such as a hidden event of a maze.
    /// </summary>
    public ValueTask OnEventAsync(Entity player, int eventId, CancellationToken cancellationToken)
    {
        return CountAsync(player, e => QuestRules.Matches(e, QuestConditionType.Clear, QuestConditionTarget.Event, eventId), 1);
    }

    /// <summary>
    /// The person has done something with items that a condition names, such as buying or making them.
    /// </summary>
    public ValueTask OnItemUsedAsync(Entity player, QuestConditionType type, int item, CancellationToken cancellationToken)
    {
        return CountAsync(player, e => QuestRules.Matches(e, type, QuestConditionTarget.Item, item), 1);
    }

    /// <summary>
    /// The person has done something a condition counts, told by a feature that can not wait for the quests: an item bought,
    /// sold, made, upgraded or taken apart (<see cref="QuestConditionTarget.Item"/>), a bonus of the skill deck. Each call
    /// counts once.
    /// </summary>
    public void OnEvent(Entity player, QuestConditionType type, QuestConditionTarget target, int id, int count = 1)
    {
        if (count <= 0 || !TryGet(player, out _, out _))
        {
            return;
        }

        Run(nameof(OnEvent), () => ProgressAsync(player, e => QuestRules.Matches(e, type, target, id), count).AsTask());
    }

    /// <summary>
    /// The person has stepped into a box of a map that belongs to a condition.
    /// </summary>
    public ValueTask OnConditionAdvancedAsync(Entity player, int condition, int count, CancellationToken cancellationToken)
    {
        return CountAsync(player, e => e.Id == condition, count);
    }

    /// <summary>
    /// A script of the maze finishes a condition, whatever its count.
    /// </summary>
    public void CompleteCondition(Entity player, int condition)
    {
        if (!catalog.TryGetCondition(condition, out var definition))
        {
            return;
        }

        Run(nameof(CompleteCondition), () => ProgressAsync(player, e => e.Id == condition, definition.Counter).AsTask());
    }

    /// <summary>
    /// The client says the person has talked to a character for a condition (<c>15/08</c>): the episode, the condition and
    /// the character. It counts when the condition is one the client tells, names that character, the person stands at
    /// the character when the server knows where it is, and has what the condition takes. False when it does not count.
    /// </summary>
    public async ValueTask<bool> OnEventUpdateAsync(Entity player, int condition, long parameter, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out _, out _) ||
            !catalog.TryGetCondition(condition, out var definition) ||
            !ToldByTheClient.Contains(definition.Type) ||
            parameter is < int.MinValue or > int.MaxValue ||
            !QuestRules.AcceptsParameter(definition, (int)parameter))
        {
            return false;
        }

        if (definition.Target == QuestConditionTarget.Npc && _npcs.Reach(player, definition.TargetId) == NpcReach.Far)
        {
            LogTooFar(logger, nameof(OnEventUpdateAsync), condition, definition.TargetId);
            return false;
        }

        // Handing something in needs the thing.
        foreach (var (item, count) in definition.TakenOnDone)
        {
            if (await items.CountAsync(player, item, cancellationToken) < count)
            {
                return false;
            }
        }

        var steps = await ProgressAsync(player, e => e.Id == condition, 1);

        return steps > 0;
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

    private async ValueTask CountAsync(Entity player, Func<QuestConditionDefinition, bool> matches, int count)
    {
        await ProgressAsync(player, matches, count);
    }

    /// <summary>
    /// Counts an act for every open condition it matches, tells the client and gives and takes what the conditions that are
    /// done name. Returns how many conditions moved.
    /// </summary>
    private async ValueTask<int> ProgressAsync(Entity player, Func<QuestConditionDefinition, bool> matches, int count, bool save = true, bool recount = true)
    {
        if (!TryGet(player, out var book, out _))
        {
            return 0;
        }

        var steps = QuestRules.Apply(book, catalog, matches, count);

        if (steps.Count == 0)
        {
            return 0;
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
            return 0;
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

        // A condition that is done counts the items held again for the collecting conditions of the person.
        if (recount && kept.Any(e => e.Completed))
        {
            await RecountHeldAsync(player, CancellationToken.None);
        }

        if (save)
        {
            await SaveAsync(player);
        }

        return kept.Count;
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

    /// <summary>
    /// The start of the game day that is running, which is when repeated episodes counted from.
    /// </summary>
    private DateTimeOffset Today() => GameDay.Get(_time.GetLocalNow(), QuestRules.RepeatResetTime).Start;

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
    /// Refuses taking an episode: the client is told when it asked, the log has it otherwise.
    /// </summary>
    private void Refuse(Entity player, QuestOpcode command, ushort code, bool tell)
    {
        if (tell)
        {
            SendError(player, command, code);
        }
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
        if (world.IsAlive(player))
        {
            world.MarkDirty(player, PersonSections.Quests);
        }

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Work} of {Id} refused: the person is not near character {Npc}.")]
    private static partial void LogTooFar(ILogger logger, string work, int id, int npc);

    #endregion Helpers
}
