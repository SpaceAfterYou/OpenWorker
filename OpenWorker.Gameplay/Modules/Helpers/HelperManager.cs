using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Progress;

namespace OpenWorker.Gameplay.Modules.Helpers;

/// <summary>
/// Helpers are characters that a person gets for finishing quests. They are called in mazes to fight beside the person.
/// What is kept here is the list: which helpers a person has, in which order, and whether they come by themselves.
/// Calling a helper means placing it in a maze, which is left to the maze.
/// </summary>
public sealed class HelperManager(
    World world,
    PersonStateStore store,
    HelperCatalog catalog,
    AchievementManager? achievements = null)
{
    /// <summary>
    /// Helpers are not called, nor sent away, outside a maze: "supporters cannot be summoned in town".
    /// </summary>
    public const ushort NotInMaze = 58006;

    /// <summary>
    /// The helper cannot be called in this maze, or is not called and so cannot be sent away.
    /// </summary>
    public const ushort CannotSummon = 58007;

    /// <summary>
    /// The item cannot be put on the helper.
    /// </summary>
    public const ushort CannotEquip = 58004;

    /// <summary>
    /// There is no reward for the support to take.
    /// </summary>
    public const ushort NoSupportReward = 52004;

    /// <summary>
    /// Reads the helpers of a person entering a district and tells the client.
    /// </summary>
    public ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        return LoadAsync(player, person, false, cancellationToken);
    }

    /// <summary>
    /// Reads the helpers of a person entering a maze and tells the client, which replaces the list it had.
    /// </summary>
    public ValueTask EnterMazeAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        return LoadAsync(player, person, true, cancellationToken);
    }

    private async ValueTask LoadAsync(Entity player, int person, bool inMaze, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<HelperBook>(person, PersonStateKind.Helper, cancellationToken) ?? new HelperBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        book.Normalize(catalog);

        world.Set(player, new HelperComponent { Person = person, Book = book, InMaze = inMaze });

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperListLoadResponse
            {
                Person = person,
                Helpers = book.Helpers.Select(ToValue).ToArray(),
                AutoSummon = book.AutoSummon ? (byte)1 : (byte)0
            });
    }

    /// <summary>
    /// Gives the person a helper, which is what finishing a quest that rewards one does.
    /// </summary>
    public async ValueTask<bool> AddAsync(Entity player, int helper, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book) || !catalog.Contains(helper) || !book.TryAdd(helper, out var entry))
        {
            return false;
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperAddResponse { Helper = ToValue(entry) });

        achievements?.OnHelperGained(player, helper);

        await SaveAsync(player, book, cancellationToken);

        return true;
    }

    /// <summary>
    /// The person asks to call a helper or to send it away. Outside a maze both are refused as the original refuses them;
    /// in a maze a helper is not called yet, so a call is refused as one the maze does not allow, and there is nothing
    /// to send away.
    /// </summary>
    public void Summon(Entity player, bool summon)
    {
        var inMaze = world.IsAlive(player) && world.TryGet<HelperComponent>(player, out var component) && component.InMaze;

        SendError(player, HelperOpcode.Summon, inMaze ? CannotSummon : NotInMaze);
    }

    public async ValueTask ChangeOrderAsync(Entity player, int first, int second, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book) || !book.TrySwap(first, second, out var one, out var two))
        {
            return;
        }

        var person = world.Get<HelperComponent>(player).Person;

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperChangeOrderResponse
            {
                Person = person,
                First = one.Helper,
                FirstOrder = one.Order,
                Second = two.Helper,
                SecondOrder = two.Order
            });

        await SaveAsync(player, book, cancellationToken);
    }

    public async ValueTask SetAutoSummonAsync(Entity player, int person, byte flag, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book) || flag > 1)
        {
            return;
        }

        book.AutoSummon = flag == 1;

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperAutoSummonResponse { Person = person, Flag = flag });

        await SaveAsync(player, book, cancellationToken);
    }

    public void SendSupportInfo(Entity player)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperSupportInfoResponse { Info = ToSupport(book) });
    }

    public async ValueTask RegisterSupportAsync(Entity player, byte supportType, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        book.SupportType = supportType;

        world.Get<ServerSessionComponent>(player)
            .Send(new HelperSupportRegisterResponse { Result = 0, Info = ToSupport(book) });

        await SaveAsync(player, book, cancellationToken);
    }

    /// <summary>
    /// The friends of the person that offer support. There are none until friends are kept by the server.
    /// </summary>
    public void SendSupportList(Entity player)
    {
        if (TryGet(player, out _))
        {
            world.Get<ServerSessionComponent>(player)
                .Send(new HelperSupportListResponse { Friends = [] });
        }
    }

    public void RefuseSupportReward(Entity player) => SendError(player, HelperOpcode.SupportReward, NoSupportReward);

    public void RefuseEquip(Entity player, HelperOpcode command) => SendError(player, command, CannotEquip);

    private HelperValue ToValue(HelperEntry entry)
    {
        return new HelperValue
        {
            Helper = entry.Helper,
            Order = entry.Order,
            Origin = [],
            Additional = []
        };
    }

    private static HelperSupportInfoValue ToSupport(HelperBook book)
    {
        return new HelperSupportInfoValue(0, book.SupportType, 0, DateTimeOffset.UnixEpoch, 0, book.SupportType != 0);
    }

    private void SendError(Entity player, HelperOpcode command, ushort code)
    {
        if (world.IsAlive(player))
        {
            world.Get<ServerSessionComponent>(player)
                .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Helper, command), code));
        }
    }

    private bool TryGet(Entity player, out HelperBook book)
    {
        if (world.IsAlive(player) && world.TryGet<HelperComponent>(player, out var component) && component.Book is not null)
        {
            book = component.Book;
            return true;
        }

        book = null!;
        return false;
    }

    /// <summary>
    /// The flush of the person writes the book (<see cref="PersonSections.MiscState"/>); nothing waits for the database here.
    /// </summary>
    private ValueTask SaveAsync(Entity player, HelperBook book, CancellationToken cancellationToken)
    {
        world.MarkDirty(player, PersonSections.MiscState);

        return ValueTask.CompletedTask;
    }
}
