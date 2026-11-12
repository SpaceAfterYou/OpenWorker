using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.MyRoom.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// Making the room of an account (26/10) and going into a room (26/11), from a district or from a room. A room is played in an
/// instance of its maze on a maze server; the record of the room in Redis sends everybody who comes into the same instance, and
/// the maze server checks at the door who may come in. Entering the room of a friend counts for its achievement.
/// </summary>
public sealed partial class MyRoomEntryService(
    World world,
    MyRoomBook book,
    MyRoomCatalog catalog,
    IMyRoomStore store,
    IMyRoomPlace place,
    WorldManager manager,
    IPersonRelations relations,
    MyRoomDirectory directory,
    ILogger<MyRoomEntryService> logger,
    AchievementManager? achievements = null
) :
    IHotspotHandler<MyRoomCreateRequest>,
    IHotspotHandler<MyRoomEnterRequest>
{
    private static readonly MessageOpcode EnterOpcode = MessageOpcode.From(GroupOpcode.MyRoom, MyRoomOpcode.EnterReq);

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomCreateRequest request)
    {
        await book.CreateAsync(context.Player, request.Index, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomEnterRequest request)
    {
        var player = context.Player;

        if (await CheckEnterAsync(player, request.Person, context.CancellationToken) is not { } target)
        {
            return;
        }

        if (target.Code is { } refused)
        {
            Refuse(player, refused);
            return;
        }

        var (owner, room, row) = (target.Owner, target.Room!.Value, target.Row);
        var back = place.ReturnOf(player);
        var record = await RouteAsync(owner, room.Gate);

        if (!world.IsAlive(player))
        {
            return;
        }

        var account = world.Get<ClaimsComponent>(player).Account;

        // What the maze server last told of the room; it checks again when the player comes.
        if (owner != account && (record.Arranging || record.Visitors >= row.MaxVisitors))
        {
            Refuse(player, record.Arranging ? MyRoomCodes.Arranging : MyRoomCodes.Full);
            return;
        }

        // The room of a friend counts for the achievement, the visit of one's own does not.
        if (owner != account && achievements is not null &&
            await relations.IsFriendAsync(world.Get<ActorComponent>(player).Identifier, request.Person))
        {
            if (!world.IsAlive(player))
            {
                return;
            }

            achievements.Count(player, AchievementType.RoomVisited);
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        if (!await manager.SendToRoomAsync(player, (short)row.Maze, (record.Host, record.Port), record.Instance, owner, back))
        {
            if (world.IsAlive(player))
            {
                LogNoStart(row.Maze);
                Refuse(player, MyRoomCodes.EnterFailed);
            }

            return;
        }

        await place.LeftAsync(player);
    }

    private readonly record struct EnterTarget(ushort? Code, int Owner = 0, MyRoomRecord? Room = null, UpdateContent.Res.Rows.MyroomInfoRow Row = default);

    /// <summary>
    /// The room of the account of the character asked for, or why the player may not go into it. Nothing when the player left
    /// meanwhile.
    /// </summary>
    private async ValueTask<EnterTarget?> CheckEnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return null;
        }

        if (person <= 0)
        {
            return new EnterTarget(MyRoomCodes.EnterFailed);
        }

        // A room is entered from a district or from a room, not from a maze.
        if (place.RoomOf(player) is not { } standing)
        {
            return new EnterTarget(MyRoomCodes.WrongPlace);
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var own = person == world.Get<ActorComponent>(player).Identifier;

        int owner;
        MyRoomRecord? room;

        if (own)
        {
            owner = account;
            room = book.RoomOf(player);
        }
        else
        {
            var found = await store.AccountOfAsync(person, book.Gate, cancellationToken);

            if (!world.IsAlive(player))
            {
                return null;
            }

            if (found is not { } other)
            {
                return new EnterTarget(MyRoomCodes.EnterFailed);
            }

            owner = other;
            room = other == account ? book.RoomOf(player) : await store.FindAsync(other, book.Gate, cancellationToken);

            if (!world.IsAlive(player))
            {
                return null;
            }
        }

        if (standing == owner)
        {
            return new EnterTarget(MyRoomCodes.AlreadyThere);
        }

        if (room is not { } made)
        {
            return new EnterTarget(owner == account ? MyRoomCodes.EnterFailed : MyRoomCodes.NoRoom);
        }

        if (owner != account && made.OpenLevel != 0)
        {
            return new EnterTarget(MyRoomCodes.Closed);
        }

        if (!catalog.TryGetRoom(made.Index, out var row) || !catalog.IsRoomMaze(row.Maze))
        {
            return new EnterTarget(MyRoomCodes.EnterFailed);
        }

        return new EnterTarget(null, owner, made, row);
    }

    /// <summary>
    /// The instance the room plays in now, or a new one on the maze server with the fewest players when nobody plays it or the
    /// server that did is gone. The record decides in one step, so two servers that send players in at once send them into one.
    /// </summary>
    private async ValueTask<MyRoomCache> RouteAsync(int owner, short gate)
    {
        return await directory.RouteAsync(owner, gate, await manager.PickMazeServerAsync());
    }

    private void Refuse(Entity player, ushort code)
    {
        if (world.IsAlive(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(EnterOpcode, code));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The maze {Maze} of the rooms has no start to enter by")]
    private partial void LogNoStart(int maze);
}
