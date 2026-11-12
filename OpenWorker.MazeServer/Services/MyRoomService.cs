using System.Numerics;
using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.MyRoom.Requests;
using OpenWorker.Hotspot.Modules.MyRoom.Responses;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// What happens in a room: who may come in and what the client is told then (26/11, 26/01, 26/24), leaving it (26/13), the
/// owner arranging its furniture (26/05, 26/20, 26/21, 26/06), naming it and closing it (26/14), sending a visitor out (26/15),
/// and the players who sit or lie on the furniture (26/22, 26/23). A player sent out is told why (26/25) and goes back to the
/// district it came from.
/// </summary>
public sealed partial class MyRoomService(
    World world,
    MyRooms rooms,
    MyRoomBook book,
    MyRoomCatalog catalog,
    IMyRoomStore store,
    MazeInstances instances,
    WorldManager manager,
    InventoryService inventory,
    IPersonFlush persons,
    ILogger<MyRoomService> logger
) :
    IHotspotHandler<MyRoomEditStartRequest>,
    IHotspotHandler<MyRoomEditEndRequest>,
    IHotspotHandler<MyRoomExitRequest>,
    IHotspotHandler<MyRoomSetupRequest>,
    IHotspotHandler<MyRoomKickOutRequest>,
    IHotspotHandler<MyRoomItemAddRequest>,
    IHotspotHandler<MyRoomItemDelRequest>,
    IHotspotHandler<MyRoomItemUseRequest>,
    IHotspotHandler<MyRoomItemReleaseUsedRequest>
{
    private static readonly MessageOpcode ItemDelOpcode = MessageOpcode.From(GroupOpcode.MyRoom, MyRoomOpcode.ItemDel);
    private static readonly MessageOpcode ItemUseOpcode = MessageOpcode.From(GroupOpcode.MyRoom, MyRoomOpcode.ItemUse);

    /// <summary>
    /// The animations a piece of the furniture table can name for the player on it.
    /// </summary>
    private const int Animations = 4;

    #region Coming in and going out

    /// <summary>
    /// A player has come into the instance of a maze. When the maze is a room, the room is opened for the first one to come,
    /// the player is let in or sent back with the reason, and the client is told the room, its furniture and who sits where.
    /// </summary>
    public async ValueTask ArriveAsync(Entity player, LuaMaze maze, int owner, short back, CancellationToken cancellationToken)
    {
        if (owner == 0 || !world.IsAlive(player))
        {
            return;
        }

        var room = rooms.Find(maze.Instance);

        if (room is null)
        {
            var record = await store.FindAsync(owner, book.Gate, cancellationToken);
            var furniture = await store.FurnitureAsync(owner, book.Gate, cancellationToken);

            if (!world.IsAlive(player))
            {
                return;
            }

            if (record is not { } made || !catalog.TryGetRoom(made.Index, out var row))
            {
                await SendOutAsync(player, back, MyRoomCodes.EnterFailed);
                return;
            }

            // Another player may have opened it while the room was read.
            room = rooms.Find(maze.Instance);

            if (room is null)
            {
                room = new MyRoomState(made, row.MaxVisitors, furniture, catalog);
                rooms.Open(maze.Instance, room);
            }
        }

        var account = world.Get<ClaimsComponent>(player).Account;

        if (room.Admit(account) is { } refused)
        {
            await SendOutAsync(player, back, refused);
            return;
        }

        rooms.Join(player, maze.Instance, room, account, world.Get<ActorComponent>(player).Identifier, back);

        var session = world.Get<ServerSessionComponent>(player);

        session.Send(new MyRoomEnterResponse { Room = book.OwnerOf(room.Room) });
        session.Send(new MyRoomLoadResponse { Furniture = room.Furniture.Select(ToValue).ToArray() });

        if (room.Seats.Count > 0)
        {
            session.Send(new MyRoomItemUsedUsersResponse
            {
                Seats = room.Seats.Select(e => new MyRoomSeatValue(e.Actor, e.Serial, e.Index, e.Animation)).ToArray()
            });
        }

        LogEntered(account, room.Room.Account, maze.Instance);

        await rooms.PublishAsync(maze.Instance, room);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomExitRequest request)
    {
        if (!rooms.TryGet(context.Player, out _, out var back))
        {
            return;
        }

        await SendOutAsync(context.Player, back, null);
    }

    /// <summary>
    /// The owner sends a visitor out of its room.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomKickOutRequest request)
    {
        if (OwnRoom(context.Player) is not { } room)
        {
            return;
        }

        var visitor = room.Visitors.FirstOrDefault(e => world.IsAlive(e) && world.Get<ActorComponent>(e).Identifier == request.Actor);

        if (visitor != Entity.Null && rooms.TryGet(visitor, out _, out var back))
        {
            await SendOutAsync(visitor, back, MyRoomCodes.KickedOut);
        }
    }

    /// <summary>
    /// Tells the player why (when there is a reason) and sends it back to the district it came from; the room forgets it as it
    /// leaves the instance.
    /// </summary>
    private async ValueTask SendOutAsync(Entity player, short back, ushort? reason)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        if (reason is { } code)
        {
            world.Get<ServerSessionComponent>(player).Send(new MyRoomOutReasonResponse { Reason = code });
        }

        if (!await manager.TryEnter(player, back))
        {
            LogNoWayBack(back);
            return;
        }

        await instances.LeaveAsync(player);
    }

    #endregion

    #region Arranging

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomEditStartRequest request)
    {
        if (OwnRoom(context.Player) is not { } room || !room.StartArranging())
        {
            return;
        }

        var instance = world.Get<LuaMaze>(context.Player).Instance;

        // Nobody else stays while the owner arranges.
        foreach (var visitor in room.Visitors.ToArray())
        {
            if (rooms.TryGet(visitor, out _, out var back))
            {
                await SendOutAsync(visitor, back, MyRoomCodes.Arranging);
            }
        }

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        await rooms.PublishAsync(instance, room);

        Send(context.Player, new MyRoomEditStartResponse());
    }

    /// <summary>
    /// The owner is done: the pieces that moved are kept where they stand now, and the room opens again.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomEditEndRequest request)
    {
        if (OwnRoom(context.Player) is not { } room)
        {
            return;
        }

        var instance = world.Get<LuaMaze>(context.Player).Instance;
        var moved = room.Arranging ? room.Move(request.Pieces.Select(e => new MyRoomPiece(e.Serial, e.Item, e.Rotation, e.Grid))) : [];

        room.StopArranging();

        await store.MoveAsync(room.Room.Account, room.Room.Gate, moved, context.CancellationToken);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        await rooms.PublishAsync(instance, room);

        Send(context.Player, new MyRoomEditEndResponse());
    }

    /// <summary>
    /// The owner places a piece from its furniture bag: the item leaves the bag and stands in the room under its serial.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomItemAddRequest request)
    {
        var player = context.Player;
        var piece = request.Piece;

        if (OwnRoom(player) is not { Arranging: true } room || room.Has(piece.Serial) || piece.Grid < 0 ||
            FindInBag(player, piece.Serial) is not { } item || item.Item != piece.Item || item.Count != 1 ||
            !catalog.TryGetFurniture(item.Item, out _))
        {
            return;
        }

        var placed = new MyRoomPiece(piece.Serial, piece.Item, piece.Rotation, piece.Grid);

        inventory.Spend(player, item, 1);
        room.Place(placed);

        if (!await store.PlaceAsync(room.Room.Account, room.Room.Gate, placed, context.CancellationToken))
        {
            LogNotPlaced(piece.Serial, room.Room.Account);
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        // The bag without the item is written at once, so the piece is never in the room and in the bag both.
        await persons.FlushAsync(player);

        Send(player, new MyRoomItemAddResponse { Piece = ToValue(placed) });
    }

    /// <summary>
    /// The owner takes a piece back into its furniture bag, if the bag has a free slot.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomItemDelRequest request)
    {
        var player = context.Player;

        if (OwnRoom(player) is not { Arranging: true } room || room.Furniture.FirstOrDefault(e => e.Serial == request.Serial) is not { Serial: not 0 } piece)
        {
            return;
        }

        // The piece comes into the bag before it leaves the room: a refused gift changes nothing.
        if (!inventory.TryGive(player, piece.Item, 1))
        {
            Send(player, new ErrorMessageResponse(ItemDelOpcode, MyRoomCodes.BagFull));
            return;
        }

        room.TryRemove(piece.Serial, out _);

        await store.RemoveAsync(room.Room.Account, room.Room.Gate, piece.Serial, context.CancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        await persons.FlushAsync(player);

        Send(player, new MyRoomItemDelResponse { Serial = piece.Serial });
    }

    #endregion

    #region Naming

    /// <summary>
    /// The owner names its room and opens or closes it; everybody in it is told, and a closed room sends its visitors out.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomSetupRequest request)
    {
        var player = context.Player;
        var name = request.Name ?? string.Empty;

        if (OwnRoom(player) is not { } room || request.OpenLevel > 1 || name.Length == 0 || name.Length > MyRoomBook.MaxName)
        {
            return;
        }

        var instance = world.Get<LuaMaze>(player).Instance;

        room.Setup(request.OpenLevel, name);
        book.Remember(player, room.Room);

        await store.SetupAsync(room.Room.Account, room.Room.Gate, request.OpenLevel, name, context.CancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        Broadcast(room, new MyRoomSetupResponse { OpenLevel = request.OpenLevel, Name = name });

        if (request.OpenLevel != 0)
        {
            foreach (var visitor in room.Visitors.ToArray())
            {
                if (rooms.TryGet(visitor, out _, out var back))
                {
                    await SendOutAsync(visitor, back, MyRoomCodes.Closed);
                }
            }
        }

        await rooms.PublishAsync(instance, room);
    }

    #endregion

    #region Sitting

    /// <summary>
    /// A player sits or lies on a piece: it takes a free place of it and stands where the piece is; everybody in the room sees
    /// it. The player who asks is the one who sits, whatever actor the request names.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomItemUseRequest request)
    {
        var player = context.Player;

        if (!rooms.TryGet(player, out var room, out _) || !world.IsAlive(player))
        {
            return ValueTask.CompletedTask;
        }

        var place = world.Get<WorldComponent>(player);
        var position = IsFinite(request.Position) ? request.Position : place.Position;
        var rotation = float.IsFinite(request.Rotation) ? request.Rotation : place.Rotation;
        var actor = world.Get<ActorComponent>(player).Identifier;

        if (request.Animation >= Animations || !IsFinite(request.FurniturePosition) || !float.IsFinite(request.FurnitureRotation) ||
            room.Sit(actor, request.Serial, request.Animation, position, rotation, Random.Shared) is not { } seat)
        {
            Send(player, new ErrorMessageResponse(ItemUseOpcode, MyRoomCodes.NoSeat));
            return ValueTask.CompletedTask;
        }

        world.Set(player, place with { Position = request.FurniturePosition, Rotation = request.FurnitureRotation });

        Broadcast(room, new MyRoomItemUseResponse
        {
            Actor = actor,
            Serial = seat.Serial,
            Position = position,
            Index = seat.Index,
            Animation = seat.Animation
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A player gets up and stands where it stood before it sat down; everybody in the room sees it.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, MyRoomItemReleaseUsedRequest request)
    {
        var player = context.Player;

        if (!rooms.TryGet(player, out var room, out _) || !world.IsAlive(player) ||
            room.Stand(world.Get<ActorComponent>(player).Identifier) is not { } seat)
        {
            return ValueTask.CompletedTask;
        }

        world.Set(player, world.Get<WorldComponent>(player) with { Position = seat.Position, Rotation = seat.Rotation });

        Broadcast(room, new MyRoomItemReleaseUsedResponse
        {
            Actor = seat.Actor,
            Serial = seat.Serial,
            Position = seat.Position,
            Rotation = seat.Rotation
        });

        return ValueTask.CompletedTask;
    }

    #endregion

    /// <summary>
    /// The room the player stands in, when it is the room of its account.
    /// </summary>
    private MyRoomState? OwnRoom(Entity player)
    {
        return world.IsAlive(player) && world.Has<LuaMaze>(player) && rooms.TryGet(player, out var room, out _) &&
               room.IsOwner(world.Get<ClaimsComponent>(player).Account)
            ? room
            : null;
    }

    private ItemInstance? FindInBag(Entity player, long serial)
    {
        return inventory.InventoryOf(player)?.Find(StorageGroup.Cube)?.Slots.FirstOrDefault(e => e is not null && e.Serial == serial);
    }

    private static MyRoomFurnitureValue ToValue(MyRoomPiece piece) => new(piece.Serial, piece.Item, piece.Rotation, piece.Grid);

    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    private void Broadcast<T>(MyRoomState room, T message) where T : IResponseHotspotMessage
    {
        foreach (var member in room.Members)
        {
            Send(member, message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {Account} came into the room of account {Owner}, instance {Instance}")]
    private partial void LogEntered(int account, int owner, Guid instance);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A player in a room could not be sent back to district {District}")]
    private partial void LogNoWayBack(short district);

    [LoggerMessage(Level = LogLevel.Error, Message = "The piece {Serial} placed in the room of account {Account} stands in a room already")]
    private partial void LogNotPlaced(long serial, int account);
}
