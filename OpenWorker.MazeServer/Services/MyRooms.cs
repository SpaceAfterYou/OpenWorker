using Arch.Core;
using OpenWorker.Gameplay.Modules.MyRoom;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The rooms this maze server runs, by the instance of the maze they are played in, and the players in them with the district
/// each goes back to. The record of a room in Redis follows what the room is: how many visitors, whether it is arranged, and it
/// goes when the last player leaves. Everything runs on the world loop.
/// </summary>
public sealed partial class MyRooms(IRedisCollection<MyRoomCache> records, ILogger<MyRooms> logger)
{
    private readonly record struct Visit(Guid Instance, short Back, int Actor);

    private readonly Dictionary<Guid, MyRoomState> _rooms = [];
    private readonly Dictionary<Entity, Visit> _players = [];

    public MyRoomState? Find(Guid instance) => _rooms.GetValueOrDefault(instance);

    public void Open(Guid instance, MyRoomState room) => _rooms[instance] = room;

    /// <summary>
    /// The room the player stands in and the district it goes back to.
    /// </summary>
    public bool TryGet(Entity player, out MyRoomState room, out short back)
    {
        if (_players.TryGetValue(player, out var visit) && _rooms.TryGetValue(visit.Instance, out var found))
        {
            room = found;
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
        if (!_players.Remove(player, out var visit) || !_rooms.TryGetValue(visit.Instance, out var room))
        {
            return;
        }

        if (room.Leave(player, visit.Actor))
        {
            await EndedAsync(visit.Instance, room.Room.Account, room.Room.Gate);
        }
        else
        {
            await PublishAsync(visit.Instance, room);
        }
    }

    /// <summary>
    /// Tells the record of the room how many visitors it has and whether it is arranged, for the servers that send players in.
    /// </summary>
    public async Task PublishAsync(Guid instance, MyRoomState room)
    {
        try
        {
            var key = MyRoomCache.KeyOf(room.Room.Account, room.Room.Gate);

            if (await records.FindByIdAsync(key) is { } record && record.Instance == instance)
            {
                record.Arranging = room.Arranging;
                record.Visitors = room.VisitorCount;

                await records.UpdateAsync(record);
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The maze server checks at the door: a record a moment behind lets a player come who is then sent back.
            LogNotPublished(e, room.Room.Account);
        }
    }

    /// <summary>
    /// The instance of a room ended: its record goes, if it is still the record of this instance.
    /// </summary>
    private async ValueTask EndedAsync(Guid instance, int account, short gate)
    {
        _rooms.Remove(instance);

        try
        {
            if (await records.FindByIdAsync(MyRoomCache.KeyOf(account, gate)) is { } record && record.Instance == instance)
            {
                await records.DeleteAsync(record);
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The record expires by itself.
            LogNotRemoved(e, account);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the room of account {Account} could not be updated")]
    private partial void LogNotPublished(Exception exception, int account);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the room of account {Account} could not be removed, it expires by itself")]
    private partial void LogNotRemoved(Exception exception, int account);
}
