using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Stores;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.MyRoom.Responses;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Gameplay.Modules.MyRoom;

/// <summary>
/// The room of the account of a person, as the person carries it from server to server: read when the person enters and
/// told to the client (26/12), and made at the request of the person (26/10). A room belongs to the account on its gate:
/// every character of the account has the same one.
/// </summary>
public sealed partial class MyRoomBook(
    World world,
    IMyRoomStore store,
    MyRoomCatalog catalog,
    InventoryService inventory,
    IOptions<GateOptions> gate,
    ILogger<MyRoomBook> logger)
{
    private static readonly MessageOpcode CreateOpcode = MessageOpcode.From(GroupOpcode.MyRoom, MyRoomOpcode.CreateReq);

    public short Gate { get; } = gate.Value.Id;

    /// <summary>
    /// Reads the room of the account as the person enters the server, and tells the client about it when there is one.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var room = await store.FindAsync(account, Gate, cancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        world.Set(player, new MyRoomComponent { Room = room });

        if (room is { } made)
        {
            Send(player, new MyRoomLoadIndexResponse { Room = OwnerOf(made) });
        }
    }

    /// <summary>
    /// The room of the account as the person's server knows it.
    /// </summary>
    public MyRoomRecord? RoomOf(Entity player)
    {
        return world.IsAlive(player) && world.TryGet<MyRoomComponent>(player, out var component) ? component.Room : null;
    }

    /// <summary>
    /// The room as the client reads it.
    /// </summary>
    public MyRoomOwnerValue OwnerOf(MyRoomRecord room)
    {
        catalog.TryGetRoom(room.Index, out var row);

        return new MyRoomOwnerValue
        {
            Owner = room.Account,
            Index = room.Index,
            Grid = room.Grid,
            Maze = row.Maze,
            OpenLevel = room.OpenLevel,
            Name = room.Name
        };
    }

    /// <summary>
    /// Why the person may not make the room of the row, in the order the checks are made, or nothing when it may.
    /// </summary>
    public ushort? CheckCreate(Entity player, int index)
    {
        if (RoomOf(player) is not null)
        {
            return MyRoomCodes.AlreadyMade;
        }

        if (!catalog.TryGetRoom(index, out var row) || !catalog.IsRoomMaze(row.Maze))
        {
            return MyRoomCodes.CreateFailed;
        }

        if (world.Get<LevelComponent>(player).Level < row.CreateLevel)
        {
            return MyRoomCodes.LevelTooLow;
        }

        if (inventory.Gold(player) < row.CreateGold)
        {
            return MyRoomCodes.NotEnoughGold;
        }

        return null;
    }

    /// <summary>
    /// Makes the room of the account from a row of the room table, for its gold. The room is named after the person who makes it
    /// and is open to others.
    /// </summary>
    public async ValueTask CreateAsync(Entity player, int index, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        if (CheckCreate(player, index) is { } refused)
        {
            Send(player, new ErrorMessageResponse(CreateOpcode, refused));
            return;
        }

        catalog.TryGetRoom(index, out var row);

        // The gold goes first, so a person who leaves while the room is written has paid for it.
        if (!inventory.TryAddGold(player, -row.CreateGold))
        {
            Send(player, new ErrorMessageResponse(CreateOpcode, MyRoomCodes.NotEnoughGold));
            return;
        }

        var account = world.Get<ClaimsComponent>(player).Account;
        var name = world.Get<PersonInfoComponent>(player).Name;

        if (name.Length > MaxName)
        {
            name = name[..MaxName];
        }

        var room = new MyRoomRecord(account, Gate, (short)row.Id, Grid: 1, OpenLevel: 0, name);
        var made = await store.CreateAsync(room, cancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        if (!made)
        {
            // Another character of the account made it on another server a moment before.
            inventory.TryAddGold(player, row.CreateGold);
            Send(player, new ErrorMessageResponse(CreateOpcode, MyRoomCodes.AlreadyMade));
            return;
        }

        world.Set(player, new MyRoomComponent { Room = room });

        LogCreated(account, Gate, row.Id);

        Send(player, new MyRoomCreateResponse { Room = OwnerOf(room) });
    }

    /// <summary>
    /// The room changed on the server it is played on: the person who owns it keeps the new state.
    /// </summary>
    public void Remember(Entity player, MyRoomRecord room)
    {
        if (world.IsAlive(player) && world.Has<MyRoomComponent>(player))
        {
            world.Set(player, new MyRoomComponent { Room = room });
        }
    }

    /// <summary>
    /// The longest name the client lets a room have.
    /// </summary>
    public const int MaxName = 20;

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {Account} made its room on gate {Gate} from row {Row}")]
    private partial void LogCreated(int account, short gate, int row);
}
