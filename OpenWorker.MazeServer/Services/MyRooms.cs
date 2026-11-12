using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// How an instance asked for a room: it holds it now, under <see cref="Holder"/>, or the record names
/// <see cref="Elsewhere"/>, the instance that plays the room.
/// </summary>
public readonly record struct MyRoomClaim(Guid Holder, MyRoomCache? Elsewhere);

/// <summary>
/// The rooms this maze server runs, by the instance of the maze they are played in, and the players in them with the district
/// each goes back to. A room is open in an instance only while the record of the room names that instance: it is taken as the
/// first player comes, renewed as the room changes and on a timer, and given back when the last player leaves or the instance
/// ends. The record also follows how many visitors are in and whether the room is arranged. Everything runs on the world loop.
/// </summary>
public sealed partial class MyRooms(
    MyRoomDirectory directory,
    IOptions<InstanceOptions> instance,
    IOptions<MazeEndpointOptions> maze,
    ILogger<MyRooms> logger)
{
    private readonly record struct Visit(Guid Instance, short Back, int Actor);

    private sealed record Held(MyRoomState Room, Guid Holder);

    private readonly Dictionary<Guid, Held> _rooms = [];
    private readonly Dictionary<Entity, Visit> _players = [];

    /// <summary>
    /// This server as the record of a room names it.
    /// </summary>
    private (Guid Server, string Host, short Port) Server { get; } =
        (instance.Value.Identifier, instance.Value.PublicHost, (short)(instance.Value.PublicPort ?? maze.Value.Port));

    /// <summary>
    /// The instances that hold a room.
    /// </summary>
    public IReadOnlyCollection<Guid> Instances => _rooms.Keys;

    public MyRoomState? Find(Guid instance) => _rooms.GetValueOrDefault(instance)?.Room;

    /// <summary>
    /// The instance takes the room of the account before the room is opened in it. Nothing when the record could not be read:
    /// a room is not opened without it.
    /// </summary>
    public async ValueTask<MyRoomClaim?> ClaimAsync(Guid instance, int owner, short gate)
    {
        var holder = Guid.NewGuid();

        try
        {
            var elsewhere = await directory.HoldAsync(owner, gate, instance, holder, Server, arranging: false, visitors: 0, claim: true);

            return new MyRoomClaim(holder, elsewhere);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogNotClaimed(e, owner);
            return null;
        }
    }

    /// <summary>
    /// Opens the room in the instance that took it with <paramref name="holder"/>.
    /// </summary>
    public void Open(Guid instance, MyRoomState room, Guid holder) => _rooms[instance] = new Held(room, holder);

    /// <summary>
    /// The room the player stands in and the district it goes back to.
    /// </summary>
    public bool TryGet(Entity player, out MyRoomState room, out short back)
    {
        if (_players.TryGetValue(player, out var visit) && _rooms.TryGetValue(visit.Instance, out var found))
        {
            room = found.Room;
            back = visit.Back;
            return true;
        }

        room = null!;
        back = 0;
        return false;
    }

    public void Join(Entity player, Guid instance, MyRoomState room, int account, int actor, short back)
    {
        room.Join(player, account);
        _players[player] = new Visit(instance, back, actor);
    }

    /// <summary>
    /// The player has left the instance, by whatever way. The room forgets it, and a room nobody is in is closed.
    /// </summary>
    public async ValueTask LeftAsync(Entity player)
    {
        if (!_players.Remove(player, out var visit) || !_rooms.TryGetValue(visit.Instance, out var held))
        {
            return;
        }

        if (held.Room.Leave(player, visit.Actor))
        {
            await EndAsync(visit.Instance);
        }
        else
        {
            // A room another instance has taken meanwhile is found by the next renewal.
            await KeepAsync(visit.Instance);
        }
    }

    /// <summary>
    /// Renews the record of the room of the instance and tells it how many visitors it has and whether it is arranged, for the
    /// servers that send players in. The record of the instance that plays the room when it names another one, which this
    /// instance must give the room up to; nothing otherwise, also when Redis did not answer (the next renewal tries again).
    /// </summary>
    public async ValueTask<MyRoomCache?> KeepAsync(Guid instance)
    {
        if (!_rooms.TryGetValue(instance, out var held))
        {
            return null;
        }

        var room = held.Room;

        try
        {
            return await directory.HoldAsync(room.Room.Account, room.Room.Gate, instance, held.Holder, Server, room.Arranging,
                room.VisitorCount, claim: false);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The maze server checks at the door: a record a moment behind lets a player come who is then sent back.
            LogNotPublished(e, room.Room.Account);
            return null;
        }
    }

    /// <summary>
    /// The room of the instance is played elsewhere: it closes here without giving the record back, and the players in it with
    /// the districts they go back to are told, to be sent where it plays.
    /// </summary>
    public IReadOnlyList<(Entity Player, short Back)> GiveUp(Guid instance)
    {
        if (!_rooms.Remove(instance, out var held))
        {
            return [];
        }

        var players = new List<(Entity, short)>();

        foreach (var member in held.Room.Members.ToArray())
        {
            if (_players.Remove(member, out var visit))
            {
                players.Add((member, visit.Back));
            }
        }

        return players;
    }

    /// <summary>
    /// The room of the instance ended, by its last player or with the instance: its record goes, if it is still the record of
    /// this state of the room.
    /// </summary>
    public async ValueTask EndAsync(Guid instance)
    {
        if (!_rooms.Remove(instance, out var held))
        {
            return;
        }

        var room = held.Room.Room;

        try
        {
            await directory.ReleaseAsync(room.Account, room.Gate, instance, held.Holder);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The record expires by itself, and this instance takes it again if a player still comes into it.
            LogNotRemoved(e, room.Account);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the room of account {Account} could not be taken, the room is not opened")]
    private partial void LogNotClaimed(Exception exception, int account);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the room of account {Account} could not be updated")]
    private partial void LogNotPublished(Exception exception, int account);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the room of account {Account} could not be removed, it expires by itself")]
    private partial void LogNotRemoved(Exception exception, int account);
}
