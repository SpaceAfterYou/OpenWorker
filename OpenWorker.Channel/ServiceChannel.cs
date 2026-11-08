using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Channel;

/// <summary>
/// The players of one channel of this server, and who of them sees whom. Everything but <see cref="Online"/> belongs to
/// the world loop.<br/>
/// The map of the channel is cut into square cells of <see cref="CellSize"/> units. A player sees the players of its own
/// cell and of the eight around it, so everything within one cell size and nothing beyond twice its diagonal; the relation
/// is symmetric. A player is told about another one (<see cref="WorldInInfoPcResponse"/>) when they come to see each
/// other and that it is gone (<see cref="WorldOutInfoPcResponse"/>) when they no longer do, on joining, leaving and
/// crossing the edge of a cell. What a player does in the world (<see cref="BroadcastNearby{T}"/>) goes to those who see
/// it; <see cref="Broadcast"/> still goes to the whole channel.
/// </summary>
public sealed class ServiceChannel
{
    // The largest cell index: far enough for any map, and its neighbours stay within an int.
    private const int MaxCell = 1 << 24;

    // The client reads the count of the actors of WorldOutInfoPc from one byte.
    private const int MaxActorsPerMessage = byte.MaxValue;

    // Touched on the world loop only.
    private readonly Dictionary<Entity, Member> _members = [];
    private readonly Dictionary<long, Cell> _cells = [];
    private readonly List<ActorValue> _actors = [];

    // The number of members, written on the loop at every join and leave and read from anywhere.
    private int _online;

    internal ServiceChannel(short identifier, float cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);

        Identifier = identifier;
        CellSize = cellSize;
    }

    public short Identifier { get; }

    /// <summary>
    /// The side of a cell of the visibility grid, in game units.
    /// </summary>
    public float CellSize { get; }

    /// <summary>
    /// How many players the channel has. Safe to read off the loop, by the services that publish it to Redis.
    /// </summary>
    public short Online => (short)Volatile.Read(ref _online);

    public bool Contains(Entity player) => _members.ContainsKey(player);

    public ChannelWorkload Workload => ChannelUtils.GetWorkload(_members.Count);

    public void ForEach(Action<Entity> action)
    {
        foreach (var entity in _members.Keys)
        {
            action(entity);
        }
    }

    /// <summary>
    /// Whether the two members see each other. A player sees itself.
    /// </summary>
    public bool Sees(Entity observer, Entity player)
    {
        return _members.TryGetValue(observer, out var first) &&
               _members.TryGetValue(player, out var second) &&
               Near(first.Cell, second.Cell);
    }

    /// <summary>
    /// Sends the message to every member, encoded once. For what concerns the whole channel, such as its chat.
    /// </summary>
    public void Broadcast(IResponseHotspotMessage message)
    {
        if (_members.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        if (frame is null)
        {
            return;
        }

        foreach (var (_, member) in _members)
        {
            member.Session.Send(frame);
        }
    }

    /// <summary>
    /// Sends the message to every member but the player, encoded once.
    /// </summary>
    public void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        if (_members.Count == 0)
        {
            return;
        }

        using var frame = Frame.Encode(message);

        if (frame is null)
        {
            return;
        }

        foreach (var (entity, member) in _members)
        {
            if (entity != player)
            {
                member.Session.Send(frame);
            }
        }
    }

    /// <summary>
    /// Sends what the player does to everybody who sees it, the player included, encoded once and without a box. Nobody
    /// hears a player that is not in the channel.
    /// </summary>
    public void BroadcastNearby<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        if (_members.TryGetValue(player, out var member))
        {
            SendNearby(member, in message, others: false);
        }
    }

    /// <summary>
    /// The same to everybody who sees the player but the player.
    /// </summary>
    public void BroadcastNearbyToOthers<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        if (_members.TryGetValue(player, out var member))
        {
            SendNearby(member, in message, others: true);
        }
    }

    /// <inheritdoc cref="BroadcastNearby{T}"/>
    public void BroadcastNearby(Entity player, IResponseHotspotMessage message)
    {
        if (_members.TryGetValue(player, out var member))
        {
            using var frame = Frame.Encode(message);

            SendNearby(member, frame, others: false);
        }
    }

    /// <inheritdoc cref="BroadcastNearbyToOthers{T}"/>
    public void BroadcastNearbyToOthers(Entity player, IResponseHotspotMessage message)
    {
        if (_members.TryGetValue(player, out var member))
        {
            using var frame = Frame.Encode(message);

            SendNearby(member, frame, others: true);
        }
    }

    /// <summary>
    /// Puts the player in the cell of its new place. Crossing the edge of a cell tells the members who come into sight
    /// about the player and the player about them, and those who go out of sight that the player is gone and the player
    /// that they are.
    /// </summary>
    public void Relocate(Entity player, Vector3 position, World world)
    {
        if (!_members.TryGetValue(player, out var member))
        {
            return;
        }

        var (x, y) = CellOf(position);
        var from = member.Cell;

        if (from.X == x && from.Y == y)
        {
            return;
        }

        var to = GetCell(x, y);

        // Who no longer sees the player: the cells around the old one that are not around the new one.
        _actors.Clear();

        Frame? gone = null;

        try
        {
            foreach (var cell in Around(from))
            {
                if (Near(cell, to))
                {
                    continue;
                }

                foreach (var other in cell.Members)
                {
                    if (other == member)
                    {
                        continue;
                    }

                    gone ??= Frame.Encode(new WorldOutInfoPcResponse { Actors = [member.Actor] });

                    if (gone is not null)
                    {
                        other.Session.Send(gone);
                    }

                    _actors.Add(other.Actor);
                }
            }
        }

        finally
        {
            gone?.Dispose();
        }

        SendGone(member);

        Move(member, to);

        // Who sees the player now: the cells around the new one that are not around the old one.
        Frame? came = null;
        Frame? stats = null;

        try
        {
            foreach (var cell in Around(to))
            {
                if (Near(cell, from))
                {
                    continue;
                }

                foreach (var other in cell.Members)
                {
                    if (other == member)
                    {
                        continue;
                    }

                    if (came is null)
                    {
                        (came, stats) = EncodeArrival(member, world);
                    }

                    Send(other.Session, came);
                    Send(other.Session, stats);

                    SendArrival(member.Session, other, world);
                }
            }
        }

        finally
        {
            came?.Dispose();
            stats?.Dispose();
        }
    }

    internal void Join(Entity entity, World world)
    {
        var position = world.TryGet(entity, out WorldComponent place) ? place.Position : Vector3.Zero;
        var (x, y) = CellOf(position);

        var member = new Member(entity, world.Get<ActorComponent>(entity), world.Get<ServerSessionComponent>(entity));

        // The others around are told before the player is among them, so it is not told about itself.
        var cell = GetCell(x, y);

        AnnounceArrival(member, cell, world);

        Add(member, cell);

        _members[entity] = member;

        Volatile.Write(ref _online, _members.Count);
    }

    internal void Leave(Entity player)
    {
        // A connection can close before its player has joined a channel.
        if (!_members.Remove(player, out var member))
        {
            return;
        }

        Volatile.Write(ref _online, _members.Count);

        Remove(member);

        SendNearby(member, new WorldOutInfoPcResponse { Actors = [member.Actor] }, others: true);
    }

    /// <summary>
    /// Tells the player who it sees: the answer to the request the client makes once it has loaded the map.
    /// </summary>
    public void SendOthers(Entity player, World world)
    {
        if (!_members.TryGetValue(player, out var member))
        {
            return;
        }

        var people = new List<PersonWorldPair>();

        foreach (var cell in Around(member.Cell))
        {
            foreach (var other in cell.Members)
            {
                if (other != member)
                {
                    people.Add(new PersonWorldPair(
                        PersonSnapshotMapper.CreatePersonValue(world, other.Entity, other.Entity),
                        PersonSnapshotMapper.CreateWorldValue(world, other.Entity)));
                }
            }
        }

        member.Session.Send(new WorldOtherPersonListResponse { People = people });
    }

    private void SendNearby<T>(Member member, in T message, bool others) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        using var frame = Frame.Encode(in message);

        SendNearby(member, frame, others);
    }

    private void SendNearby(Member member, Frame? frame, bool others)
    {
        if (frame is null)
        {
            return;
        }

        foreach (var cell in Around(member.Cell))
        {
            foreach (var other in cell.Members)
            {
                if (!others || other != member)
                {
                    other.Session.Send(frame);
                }
            }
        }
    }

    /// <summary>
    /// Tells everybody who sees the cell about the player that comes into it.
    /// </summary>
    private void AnnounceArrival(Member member, Cell cell, World world)
    {
        Frame? came = null;
        Frame? stats = null;

        try
        {
            foreach (var around in Around(cell))
            {
                foreach (var other in around.Members)
                {
                    if (came is null)
                    {
                        (came, stats) = EncodeArrival(member, world);
                    }

                    Send(other.Session, came);
                    Send(other.Session, stats);
                }
            }
        }

        finally
        {
            came?.Dispose();
            stats?.Dispose();
        }
    }

    private static (Frame? Came, Frame? Stats) EncodeArrival(Member member, World world)
    {
        var came = Frame.Encode(new WorldInInfoPcResponse
        {
            Person = PersonSnapshotMapper.CreatePersonValue(world, member.Entity, member.Entity),
            World = PersonSnapshotMapper.CreateWorldValue(world, member.Entity)
        });

        var stats = Frame.Encode(new PersonUpdateOriginStatListResponse { Actor = member.Actor, Stats = [] });

        return (came, stats);
    }

    /// <summary>
    /// Tells the session about a player it has come to see.
    /// </summary>
    private static void SendArrival(ServerSessionComponent session, Member other, World world)
    {
        var (came, stats) = EncodeArrival(other, world);

        using (came)
        using (stats)
        {
            Send(session, came);
            Send(session, stats);
        }
    }

    /// <summary>
    /// Tells the member that the players gathered in <see cref="_actors"/> are out of its sight.
    /// </summary>
    private void SendGone(Member member)
    {
        for (var start = 0; start < _actors.Count; start += MaxActorsPerMessage)
        {
            var count = Math.Min(MaxActorsPerMessage, _actors.Count - start);

            member.Session.Send(new WorldOutInfoPcResponse { Actors = _actors.GetRange(start, count).ToArray() });
        }

        _actors.Clear();
    }

    private static void Send(ServerSessionComponent session, Frame? frame)
    {
        if (frame is not null)
        {
            session.Send(frame);
        }
    }

    private (int X, int Y) CellOf(Vector3 position)
    {
        return (Index(position.X), Index(position.Y));
    }

    private int Index(float coordinate)
    {
        // A place is finite (Place.IsValid); a far one still lands in a cell, at the edge of the grid.
        return (int)Math.Clamp(MathF.Floor(coordinate / CellSize), -MaxCell, MaxCell);
    }

    private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    private Cell GetCell(int x, int y)
    {
        var key = Key(x, y);

        if (!_cells.TryGetValue(key, out var cell))
        {
            cell = new Cell(x, y);
            _cells.Add(key, cell);
        }

        return cell;
    }

    private static bool Near(Cell first, Cell second)
    {
        return Math.Abs(first.X - second.X) <= 1 && Math.Abs(first.Y - second.Y) <= 1;
    }

    /// <summary>
    /// The cell and the eight around it that have ever held a player.
    /// </summary>
    private Neighbourhood Around(Cell cell) => new(_cells, cell.X, cell.Y);

    private static void Add(Member member, Cell cell)
    {
        member.Cell = cell;
        member.Slot = cell.Members.Count;
        cell.Members.Add(member);
    }

    private static void Remove(Member member)
    {
        var members = member.Cell.Members;
        var last = members[^1];

        members[member.Slot] = last;
        last.Slot = member.Slot;
        members.RemoveAt(members.Count - 1);
    }

    private static void Move(Member member, Cell cell)
    {
        Remove(member);
        Add(member, cell);
    }

    /// <summary>
    /// A member of the channel: what sending to it and placing it needs, so nothing is looked up in the world on the way.
    /// </summary>
    private sealed class Member(Entity entity, ActorValue actor, ServerSessionComponent session)
    {
        public Entity Entity { get; } = entity;

        public ActorValue Actor { get; } = actor;

        public ServerSessionComponent Session { get; } = session;

        public Cell Cell { get; set; } = null!;

        /// <summary>
        /// Where the member is in the list of its cell, so it leaves the cell without a search.
        /// </summary>
        public int Slot { get; set; }
    }

    private sealed class Cell(int x, int y)
    {
        public int X { get; } = x;

        public int Y { get; } = y;

        public List<Member> Members { get; } = [];
    }

    /// <summary>
    /// The nine cells around one, walked without an allocation; the cells nobody has entered yet are skipped.
    /// </summary>
    private readonly struct Neighbourhood(Dictionary<long, Cell> cells, int x, int y)
    {
        public Enumerator GetEnumerator() => new(cells, x, y);

        public struct Enumerator(Dictionary<long, Cell> cells, int x, int y)
        {
            private int _index = -1;

            public Cell Current { get; private set; } = null!;

            public bool MoveNext()
            {
                while (++_index < 9)
                {
                    if (cells.TryGetValue(Key(x + (_index % 3) - 1, y + (_index / 3) - 1), out var cell))
                    {
                        Current = cell;

                        return true;
                    }
                }

                return false;
            }
        }
    }
}
