using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// The share points of a person: what it has done in each district and each group of mazes, told to the client when the
/// person enters a district, grown when a maze is cleared and rewarded from the window of the achievements. A change marks
/// <see cref="PersonSections.Progress"/> and the flush writes the book, in the maze as in the district.
/// </summary>
public sealed partial class SharePointManager(
    World world,
    PersonStateStore store,
    SharePointCatalog catalog,
    ProgressItems items,
    ILogger<SharePointManager> logger)
{
    /// <summary>
    /// The most districts the client takes in one list: it counts them in a signed byte.
    /// </summary>
    private const int MaxDistricts = sbyte.MaxValue;

    /// <summary>
    /// The most groups the client takes in one list: it counts them in a signed short.
    /// </summary>
    private const int MaxGroups = short.MaxValue;

    /// <summary>
    /// The book of the share points as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<SharePointComponent>(PersonStateKind.SharePoints, PersonSections.Progress, e => e.Book);

    /// <summary>
    /// Loads the share points of the person as it enters a district and tells the client every district and group, the
    /// districts first: the client files a group under its district. The district the person is in gets a state when it has
    /// none. The client takes the lists only as it enters the world from the gate and the change of one state only once it
    /// is in the world, so a new district is in the list and is told by itself as well.
    /// </summary>
    public async ValueTask EnterDistrictAsync(Entity player, int person, int district, CancellationToken cancellationToken)
    {
        if (await LoadAsync(player, person, cancellationToken) is not { } book)
        {
            return;
        }

        var visited = book.Visit(catalog, district);

        Send(player, new PersonLoadDistrictStateResponse
        {
            States = book.Districts.Take(MaxDistricts).Select(e => e.ToValue()).ToArray()
        });

        Send(player, new PersonLoadMazeStateResponse
        {
            States = book.Groups.Take(MaxGroups).Select(e => e.ToValue()).ToArray()
        });

        if (visited is not null)
        {
            world.MarkDirty(player, PersonSections.Progress);

            Send(player, new PersonUpdateSharePointResponse { District = visited.ToValue(), Maze = default });
        }
    }

    /// <summary>
    /// Loads the share points of the person as it enters a maze; the client keeps what it was told in the district.
    /// </summary>
    public async ValueTask EnterMazeAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        await LoadAsync(player, person, cancellationToken);
    }

    /// <summary>
    /// A maze of a group is cleared: the group and its district get their points, and the client is told.
    /// </summary>
    public void OnMazeCleared(Entity player, int maze, int group, int episode)
    {
        if (!TryGet(player, out var book) || book.Clear(catalog, maze, group, episode) is not { } cleared)
        {
            return;
        }

        world.MarkDirty(player, PersonSections.Progress);

        Send(player, new PersonUpdateSharePointResponse { District = cleared.District.ToValue(), Maze = cleared.Group.ToValue() });
    }

    /// <summary>
    /// A clear of a maze with its rank and time, kept with the progress of the mazes of the person.
    /// </summary>
    public void RecordClear(Entity player, int maze, byte rank, int seconds)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        book.Record(maze, rank, seconds);
        world.MarkDirty(player, PersonSections.Progress);
    }

    /// <summary>
    /// Whether the person has cleared the maze; false while its progress is not loaded.
    /// </summary>
    public bool HasCleared(Entity player, int maze)
    {
        return TryGet(player, out var book) && book.FindMaze(maze) is { Clears: > 0 };
    }

    /// <summary>
    /// The person takes the reward of a district (type 0) or of the steps of a group of mazes it has reached (any other
    /// type). The answer always goes, with the step the district or group is at, the result and what was given.
    /// </summary>
    public void TakeReward(Entity player, int type, int id)
    {
        if (!TryGet(player, out var book))
        {
            Send(player, Answer(id, 0, SharePointResult.NotReached, []));
            return;
        }

        if (type == 0)
        {
            TakeDistrict(player, book, id);
        }

        else
        {
            TakeGroup(player, book, id);
        }
    }

    private void TakeDistrict(Entity player, SharePointBook book, int id)
    {
        var result = book.ClaimDistrict(catalog, id, out var step, out var reward);

        if (result != SharePointResult.Given)
        {
            Send(player, Answer(id, step, result, []));
            return;
        }

        var given = items.Resolve(player, reward is { } one ? [one] : []);

        if (!items.TryGive(player, given))
        {
            Send(player, Answer(id, 0, SharePointResult.CannotGive, []));
            return;
        }

        book.FindDistrict(id)!.Rewarded = true;
        world.MarkDirty(player, PersonSections.Progress);

        LogGiven(logger, id, step);
        Send(player, Answer(id, step, SharePointResult.Given, given));
    }

    private void TakeGroup(Entity player, SharePointBook book, int id)
    {
        var result = book.ClaimGroup(catalog, id, out var step, out var rewards);

        if (result != SharePointResult.Given)
        {
            Send(player, Answer(id, step, result, []));
            return;
        }

        var state = book.FindGroup(id)!;
        var given = items.Resolve(player, rewards);

        if (!items.TryGive(player, given))
        {
            Send(player, Answer(id, state.Step, SharePointResult.CannotGive, []));
            return;
        }

        state.Step = step;
        world.MarkDirty(player, PersonSections.Progress);

        LogGiven(logger, id, step);
        Send(player, Answer(id, step, SharePointResult.Given, given));
    }

    private static PersonGetRewardSharePointResponse Answer(int id, byte step, SharePointResult result, IReadOnlyCollection<(int Item, int Count)> given)
    {
        return new PersonGetRewardSharePointResponse
        {
            Identifier = id,
            RewardStep = step,
            Result = (byte)result,
            Items = given.Take(sbyte.MaxValue).Select(e => new RewardItemValue { Item = e.Item, Count = (short)e.Count }).ToArray()
        };
    }

    private async ValueTask<SharePointBook?> LoadAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<SharePointBook>(person, PersonStateKind.SharePoints, cancellationToken) ?? new SharePointBook();

        if (!world.IsAlive(player))
        {
            return null;
        }

        book.Normalize(catalog);

        world.Set(player, new SharePointComponent { Person = person, Book = book });

        return book;
    }

    private bool TryGet(Entity player, out SharePointBook book)
    {
        if (world.IsAlive(player) && world.TryGet<SharePointComponent>(player, out var component) && component.Book is not null)
        {
            book = component.Book;
            return true;
        }

        book = null!;
        return false;
    }

    private void Send<T>(Entity player, in T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(in message);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The share point reward of {Identifier} is given up to step {Step}")]
    private static partial void LogGiven(ILogger logger, int identifier, byte step);
}
