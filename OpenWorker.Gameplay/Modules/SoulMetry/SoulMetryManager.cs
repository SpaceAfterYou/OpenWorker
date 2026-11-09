using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.SoulMetry.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Rewards.Types;
using OpenWorker.Hotspot.Modules.SoulMetry.Responses;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Gameplay.Modules.SoulMetry;

/// <summary>
/// Soul metries are sets of objects to find in mazes. The person gets one when the maze it belongs to is cleared,
/// finds the objects one by one and takes the reward when all of them are found.
/// </summary>
public sealed partial class SoulMetryManager(
    World world,
    PersonStateStore store,
    SoulMetryCatalog catalog,
    ReadOnlyCollection<QuestRewardRow> questRewards,
    IRewardGranter rewards,
    ILogger<SoulMetryManager> logger)
{
    /// <summary>
    /// The person has no loaded soul metries, so nothing can be finished.
    /// </summary>
    private const ushort NotLoaded = 50011;

    /// <summary>
    /// When set, the objects always show up in mazes. A tool for testing.
    /// </summary>
    public bool AlwaysOpen { get; set; }

    /// <summary>
    /// Loads the soul metries of the person and tells the client about them.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var state = await store.LoadAsync<SoulMetryState>(person, PersonStateKind.SoulMetry, cancellationToken) ?? new SoulMetryState();

        state.Normalize(catalog);

        world.Set(player, new SoulMetryComponent { Person = person, State = state });

        SendAll(player, state);
    }

    /// <summary>
    /// Starts a soul metry, which is what clearing its maze does.
    /// </summary>
    public async ValueTask<bool> AddAsync(Entity player, short id, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var state) || !catalog.TryGet(id, out var definition) || !state.TryAdd(definition, out var bits))
        {
            return false;
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new SoulMetryAddResponse { Data = new SoulMetryValue(id, bits) });

        await SaveAsync(player, state, cancellationToken);

        return true;
    }

    /// <summary>
    /// The person clears a maze: its soul metry starts if there is one.
    /// </summary>
    public ValueTask<bool> OnMazeClearedAsync(Entity player, int maze, CancellationToken cancellationToken)
    {
        return maze is > 0 and <= short.MaxValue
            ? AddAsync(player, (short)maze, cancellationToken)
            : ValueTask.FromResult(false);
    }

    /// <summary>
    /// The person interacts with an object of a soul metry.
    /// </summary>
    public async ValueTask<bool> CollectAsync(Entity player, short id, int obj, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var state) || !catalog.TryGet(id, out var definition) || !state.TryCollect(definition, obj, out var bits))
        {
            return false;
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new SoulMetryUpdateResponse { Data = new SoulMetryValue(id, bits) });

        await SaveAsync(player, state, cancellationToken);

        return true;
    }

    /// <summary>
    /// The objects of the soul metries that are to show up in the maze: the ones the person has not found
    /// yet and that stand in this maze, each with its own chance.
    /// </summary>
    public IReadOnlyList<int> GetObjectsToSpawn(Entity player, short id, int maze)
    {
        if (!TryGet(player, out var state) ||
            !catalog.TryGet(id, out var definition) ||
            !state.Progress.TryGetValue(id, out var bits) ||
            bits == SoulMetryDefinition.AllFound)
        {
            return [];
        }

        var result = new List<int>();

        for (var order = 0; order < definition.Objects.Count; order++)
        {
            var obj = definition.Objects[order];

            if ((bits & (1 << order)) == 0 && obj.Maze == maze && (AlwaysOpen || Random.Shared.Next(0, 10000) < obj.SpawnRate))
            {
                result.Add(obj.ObjectId);
            }
        }

        return result;
    }

    /// <summary>
    /// The request to take the reward of a finished soul metry.
    /// </summary>
    public async ValueTask CompleteAsync(Entity player, int id, CancellationToken cancellationToken)
    {
        var session = world.Get<ServerSessionComponent>(player);

        if (!TryGet(player, out var state))
        {
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.SoulMetry, SoulMetryOpcode.Complete), NotLoaded));
            return;
        }

        // The client does not get an answer for the other failures, they only happen to a client that does not
        // follow its own list.
        if (id is < 0 or > short.MaxValue || !catalog.TryGet((short)id, out var definition))
        {
            LogUnknown(id);
            return;
        }

        if (!state.IsReady(definition))
        {
            LogNotComplete(id);
            return;
        }

        if (state.IsComplete(definition))
        {
            LogAlreadyComplete(id);
            return;
        }

        if (!state.Fits(definition))
        {
            LogBitDoesNotFit(id, definition.CompleteBit);
            return;
        }

        var items = GetRewardItems(player, definition);

        // The items that did not fit leave the soul metry as it was, to be taken later.
        if (!await rewards.TryGrantAsync(player, new Reward(items), RewardSource.SoulMetry, cancellationToken))
        {
            return;
        }

        state.TrySetComplete(definition);
        state.Progress.Remove(definition.Id);

        session.Send(new SoulMetryCompleteResponse
        {
            Identifier = id,
            Rewards = items.Select(e => new RewardItemValue(e.Item, e.Count)).ToArray()
        });

        await SaveAsync(player, state, cancellationToken);
    }

    /// <summary>
    /// Forgets everything of the person. A tool for testing.
    /// </summary>
    public async ValueTask ResetAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var state))
        {
            return;
        }

        state.Progress.Clear();
        Array.Clear(state.Complete);

        world.Get<ServerSessionComponent>(player)
            .Send(new SoulMetryResetResponse());

        await SaveAsync(player, state, cancellationToken);
    }

    private RewardItem[] GetRewardItems(Entity player, SoulMetryDefinition definition)
    {
        var items = new List<RewardItem>();

        foreach (var reward in definition.Rewards)
        {
            var item = reward.Type switch
            {
                1 => reward.Item,
                2 => GetClassItem(player, reward.Item),
                _ => 0
            };

            if (item > 0)
            {
                items.Add(new RewardItem(item, reward.Count));
            }
        }

        return items.ToArray();
    }

    // The quest reward table has an item for every class.
    private int GetClassItem(Entity player, int row)
    {
        if (!world.TryGet<PersonInfoComponent>(player, out var info))
        {
            return 0;
        }

        foreach (var reward in questRewards)
        {
            if (reward.Id != row)
            {
                continue;
            }

            return info.Hero switch
            {
                Hero.Haru => reward.Field1,
                Hero.Erwin => reward.Field2,
                Hero.Lily => reward.Field3,
                Hero.Jin => reward.Field4,
                Hero.Stella => reward.Field5,
                Hero.Iris => reward.Field6,
                _ => 0
            };
        }

        return 0;
    }

    private void SendAll(Entity player, SoulMetryState state)
    {
        var session = world.Get<ServerSessionComponent>(player);

        // The list comes first: the client forgets what it knows when it gets it.
        session.Send(new SoulMetryListResponse
        {
            Values = state.Progress
                .OrderBy(e => e.Key)
                .Select(e => new SoulMetryValue(e.Key, e.Value))
                .ToArray()
        });

        session.Send(new SoulMetryCompleteListResponse { Complete = state.Complete });
    }

    private bool TryGet(Entity player, out SoulMetryState state)
    {
        if (world.IsAlive(player) && world.TryGet<SoulMetryComponent>(player, out var component) && component.State is not null)
        {
            state = component.State;
            return true;
        }

        state = null!;
        return false;
    }

    /// <summary>
    /// The flush of the person writes the state (<see cref="PersonSections.MiscState"/>); nothing waits for the database here.
    /// </summary>
    private ValueTask SaveAsync(Entity player, SoulMetryState state, CancellationToken cancellationToken)
    {
        world.MarkDirty(player, PersonSections.MiscState);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Soul metry {Id} is unknown.")]
    private partial void LogUnknown(int id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Soul metry {Id} is not complete.")]
    private partial void LogNotComplete(int id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Soul metry {Id} is already complete.")]
    private partial void LogAlreadyComplete(int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Soul metry {Id} has the bit {Bit} that does not fit the list.")]
    private partial void LogBitDoesNotFit(int id, int bit);
}
