using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SocialItems.Requests;
using OpenWorker.Hotspot.Modules.SocialItems.Responses;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.SocialItems;

/// <summary>
/// The social objects of the channels of a district: a player puts one down with an item, everybody who sees it may use it,
/// and it goes when its time runs out, when its owner leaves the channel or puts down another. An object is told to the
/// players who see its place, by the cells of the channel's visibility grid: when it comes, when a player comes into sight of
/// it, and when it goes. Everything here belongs to the world loop, and a check and the change it allows run without an await
/// between them, so the limits of a channel hold.
/// </summary>
public sealed class SocialItems : ISessionCloseHandler
{
    /// <summary>The objects of one channel at most, as in the original.</summary>
    public const int MaxPerChannel = 20;

    // How far, along any axis, a player may be from the place it puts an object down or uses one at.
    private const float Reach = 1000f;

    // The actors of objects have a range of their own, apart from players and creatures.
    private const int ActorRange = 3 << 30;
    private const int ActorMask = (1 << 29) - 1;

    // The largest cell index, as the channel's grid has it.
    private const int MaxCell = 1 << 24;

    private readonly World _world;
    private readonly InventoryService _inventory;
    private readonly ItemCatalog _catalog;
    private readonly RowIndex<ushort, SocialItemRuleRow> _rules;
    private readonly ISocialItemGrounds _grounds;
    private readonly SimulationClock _clock;
    private readonly IItemLocks? _locks;
    private readonly float _cellSize;

    private readonly Dictionary<short, List<SocialObject>> _channels = [];
    private readonly Dictionary<Entity, Person> _persons = [];
    private int _next;

    public SocialItems(World world, InventoryService inventory, ItemCatalog catalog, ReadOnlyCollection<SocialItemRuleRow> rules,
        ISocialItemGrounds grounds, SimulationClock clock, IOptions<WorldOptions> sight, IItemLocks? locks = null)
    {
        _world = world;
        _inventory = inventory;
        _catalog = catalog;
        _rules = rules.IndexBy(e => e.Id);
        _grounds = grounds;
        _clock = clock;
        _locks = locks;
        _cellSize = sight.Value.VisibilityRadius;
    }

    /// <summary>
    /// What this server knows of a player: its channel, the cell it was last seen in, the objects it has been told about, the
    /// object it owns and the one it uses.
    /// </summary>
    private sealed class Person(int actor, ServerSessionComponent session)
    {
        public int Actor { get; } = actor;

        public ServerSessionComponent Session { get; } = session;

        public short Channel { get; set; }

        /// <summary>The player has asked what is around it in this channel, and is told about objects since.</summary>
        public bool Watching { get; set; }

        public int CellX { get; set; }

        public int CellY { get; set; }

        public HashSet<int> Seen { get; } = [];

        public SocialObject? Owned { get; set; }

        public SocialObject? Using { get; set; }
    }

    /// <summary>
    /// The objects of a channel.
    /// </summary>
    public IReadOnlyList<SocialObject> In(short channel) => _channels.TryGetValue(channel, out var list) ? list : [];

    /// <summary>
    /// The object the player put down, if it still stands.
    /// </summary>
    public SocialObject? OwnedBy(Entity player) => _persons.TryGetValue(player, out var person) ? person.Owned : null;

    /// <summary>
    /// The player puts an object down with an item of its bag (2D/01). Zero when it stands; otherwise the code of the refusal,
    /// in the order the original checks.
    /// </summary>
    public async ValueTask<ushort> StartAsync(Entity player, SocialItemStartRequest request)
    {
        var ground = await _grounds.GetAsync();

        if (!_world.IsAlive(player) || !_world.TryGet<ChannelMemberComponent>(player, out var member))
        {
            return SocialItemCodes.NotDistrict;
        }

        if (!TryItem(player, request, out var item, out var row))
        {
            return SocialItemCodes.Failed;
        }

        if (!_rules.TryGet((ushort)request.SocialItem, out var rule))
        {
            return SocialItemCodes.Unknown;
        }

        // The map's own objects, furniture and games for two are not put down by any item of this client.
        if (rule.Kind != SocialItemRuleRow.Normal)
        {
            return SocialItemCodes.Failed;
        }

        var objects = List(member.Index);

        if (objects.Count >= MaxPerChannel)
        {
            return SocialItemCodes.TooMany;
        }

        var place = request.Position;
        var radius = (float)(rule.CollisionRange / 2);

        if (!IsFinite(place) || !float.IsFinite(request.Rotation) || ground.IsExcluded(place) || !ground.HasFloor(place, radius) ||
            !Within(_world.Get<WorldComponent>(player).Position, place))
        {
            return SocialItemCodes.BadPlace;
        }

        foreach (var other in objects)
        {
            if (Vector3.Distance(other.Position, place) < other.Radius + radius)
            {
                return SocialItemCodes.BadPlace;
            }
        }

        var person = PersonOf(player, member.Index);

        if (person.Owned is { } old)
        {
            Remove(old);
        }

        var placed = new SocialObject(ActorRange | (++_next & ActorMask), member.Index, person.Actor, row.Id, rule.Id, place,
            request.Rotation, radius, rule.UseMax);

        (placed.CellX, placed.CellY) = CellOf(place);

        if (row.UsePeriodValue > 0)
        {
            var lasts = TimeSpan.FromMinutes(row.UsePeriodValue);

            placed.Ends = _clock.Now + lasts;
            placed.End = _clock.Schedule(lasts, () => Remove(placed));
        }

        objects.Add(placed);
        person.Owned = placed;

        Arrive(placed);

        _inventory.Spend(player, item, 1);

        person.Session.Send(new SocialItemStartResponse { Prop = ValueOf(placed) });

        return 0;
    }

    /// <summary>
    /// The player uses an object of its channel (2D/02), told to everybody who sees the player. Zero when it does.
    /// </summary>
    public ushort Use(Entity player, SocialItemUseRequest request)
    {
        if (!_world.IsAlive(player) || !_world.TryGet<ChannelMemberComponent>(player, out var member))
        {
            return SocialItemCodes.NotDistrict;
        }

        if (Find(member.Index, request.Prop) is not { } found)
        {
            return SocialItemCodes.Failed;
        }

        var place = request.Position;

        if (!IsFinite(place) || !float.IsFinite(request.Rotation) ||
            !Within(_world.Get<WorldComponent>(player).Position, place) || !Within(place, found.Position))
        {
            return SocialItemCodes.Failed;
        }

        var person = PersonOf(player, member.Index);

        if (person.Using is not null || !found.HasRoomFor(person.Actor))
        {
            return SocialItemCodes.Full;
        }

        found.Users.Add(person.Actor);
        person.Using = found;

        Nearby(person, place, new SocialItemUseResponse
        {
            Actor = person.Actor,
            Prop = ValueOf(found),
            Position = place,
            Rotation = request.Rotation
        }, others: false);

        return 0;
    }

    /// <summary>
    /// The player stops using its object (2D/03), told to everybody who sees the player. The original takes the object the player
    /// uses, whatever the request names. Zero when it did.
    /// </summary>
    public ushort Stop(Entity player)
    {
        if (!_world.IsAlive(player) || !_world.Has<ChannelMemberComponent>(player))
        {
            return SocialItemCodes.StopNotDistrict;
        }

        if (!_persons.TryGetValue(player, out var person) || person.Using is not { } used || !used.Users.Remove(person.Actor))
        {
            return SocialItemCodes.Unknown;
        }

        person.Using = null;

        Nearby(person, _world.Get<WorldComponent>(player).Position, new SocialItemStopResponse { Actor = person.Actor, Prop = ValueOf(used) },
            others: false);

        return 0;
    }

    /// <summary>
    /// The player has loaded its channel and asks what is around it: it is told the objects in sight, and about the objects
    /// that come and go from now on.
    /// </summary>
    public void Show(Entity player)
    {
        if (!_world.IsAlive(player) || !_world.TryGet<ChannelMemberComponent>(player, out var member) ||
            !_world.TryGet<WorldComponent>(player, out var place))
        {
            return;
        }

        var person = PersonOf(player, member.Index);

        person.Channel = member.Index;
        person.Watching = true;
        person.Seen.Clear();
        (person.CellX, person.CellY) = CellOf(place.Position);

        List<SocialItemPlacedValue>? come = null;

        foreach (var placed in In(member.Index))
        {
            if (Near(person, placed))
            {
                person.Seen.Add(placed.Actor);
                (come ??= []).Add(PlacedValueOf(placed));
            }
        }

        if (come is not null)
        {
            person.Session.Send(new SocialItemInfosResponse { Props = come });
        }
    }

    /// <summary>
    /// The player has moved: the objects that come into its sight are told to it, and those that go out of it are gone.
    /// </summary>
    public void Moved(Entity player, Vector3 position)
    {
        if (!_persons.TryGetValue(player, out var person) || !person.Watching)
        {
            return;
        }

        var (x, y) = CellOf(position);

        if (x == person.CellX && y == person.CellY)
        {
            return;
        }

        person.CellX = x;
        person.CellY = y;

        List<SocialItemPlacedValue>? come = null;

        foreach (var placed in In(person.Channel))
        {
            var near = Near(person, placed);

            if (near && person.Seen.Add(placed.Actor))
            {
                (come ??= []).Add(PlacedValueOf(placed));
            }
            else if (!near && person.Seen.Remove(placed.Actor))
            {
                person.Session.Send(new SocialItemOutResponse { Prop = ValueOf(placed) });
            }
        }

        if (come is not null)
        {
            person.Session.Send(new SocialItemInfosResponse { Props = come });
        }
    }

    /// <summary>
    /// The player leaves its channel, by another channel or by leaving the server: it stops using its object, and the object it
    /// put down goes.
    /// </summary>
    public void Leave(Entity player)
    {
        if (!_persons.Remove(player, out var person))
        {
            return;
        }

        if (person.Using is { } used && used.Users.Remove(person.Actor))
        {
            person.Using = null;

            var position = _world.IsAlive(player) && _world.TryGet<WorldComponent>(player, out var place) ? place.Position : used.Position;

            Nearby(person, position, new SocialItemStopResponse { Actor = person.Actor, Prop = ValueOf(used) }, others: true);
        }

        if (person.Owned is { } owned)
        {
            Remove(owned);
        }
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        Leave(player);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The item of the slot is one that puts down the social item the request names, and the player may use it.
    /// </summary>
    private bool TryItem(Entity player, SocialItemStartRequest request, out ItemInstance item, out ItemRow row)
    {
        row = default;
        item = null!;

        if (_inventory.InventoryOf(player)?.At(request.Storage, request.Slot) is not { Count: >= 1 } found || found.Serial != request.Serial ||
            _locks?.IsLocked(player, request.Storage, request.Slot) == true ||
            !_catalog.TryGet(found.Item, out row) || _catalog.Classify(found.Item) is not { ItemUseType: ItemUseType.SOCIAL_OBJECT })
        {
            return false;
        }

        item = found;

        return row.Id == request.Item && row.EffectValue == (ushort)request.SocialItem &&
               row.LevelLimit <= _world.Get<LevelComponent>(player).Level &&
               (row.ClassLimit == 0 || row.ClassLimit == (byte)_world.Get<PersonInfoComponent>(player).Hero);
    }

    /// <summary>
    /// Takes an object away: its owner and its users let go of it and everybody who saw it is told it is gone.
    /// </summary>
    private void Remove(SocialObject placed)
    {
        if (!_channels.TryGetValue(placed.Channel, out var objects) || !objects.Remove(placed))
        {
            return;
        }

        placed.End.Cancel();

        var gone = new SocialItemOutResponse { Prop = ValueOf(placed) };

        using var frame = Frame.Encode(in gone);

        foreach (var person in _persons.Values)
        {
            if (person.Owned == placed)
            {
                person.Owned = null;
            }

            if (person.Using == placed)
            {
                person.Using = null;
            }

            if (person.Seen.Remove(placed.Actor) && frame is not null)
            {
                person.Session.Send(frame);
            }
        }

        placed.Users.Clear();
    }

    /// <summary>
    /// Tells everybody who sees the place of a new object about it.
    /// </summary>
    private void Arrive(SocialObject placed)
    {
        var come = new SocialItemInResponse { Prop = PlacedValueOf(placed) };

        using var frame = Frame.Encode(in come);

        if (frame is null)
        {
            return;
        }

        foreach (var person in _persons.Values)
        {
            if (person.Watching && person.Channel == placed.Channel && Near(person, placed) && person.Seen.Add(placed.Actor))
            {
                person.Session.Send(frame);
            }
        }
    }

    /// <summary>
    /// Sends a message to everybody of the player's channel who sees the place, and to the player unless <paramref name="others"/>.
    /// </summary>
    private void Nearby<T>(Person player, Vector3 position, in T message, bool others) where T : IResponseHotspotMessage
    {
        using var frame = Frame.Encode(in message);

        if (frame is null)
        {
            return;
        }

        var (x, y) = CellOf(position);

        foreach (var person in _persons.Values)
        {
            if (person != player && person.Watching && person.Channel == player.Channel &&
                Math.Abs(person.CellX - x) <= 1 && Math.Abs(person.CellY - y) <= 1)
            {
                person.Session.Send(frame);
            }
        }

        if (!others)
        {
            player.Session.Send(frame);
        }
    }

    private Person PersonOf(Entity player, short channel)
    {
        if (!_persons.TryGetValue(player, out var person))
        {
            person = new Person((int)_world.Get<ActorComponent>(player), _world.Get<ServerSessionComponent>(player)) { Channel = channel };

            _persons.Add(player, person);
        }

        return person;
    }

    private List<SocialObject> List(short channel)
    {
        if (!_channels.TryGetValue(channel, out var list))
        {
            list = [];
            _channels.Add(channel, list);
        }

        return list;
    }

    private SocialObject? Find(short channel, int actor)
    {
        foreach (var placed in In(channel))
        {
            if (placed.Actor == actor)
            {
                return placed;
            }
        }

        return null;
    }

    private SocialItemValue ValueOf(SocialObject placed)
    {
        var left = placed.Ends is { } ends ? Math.Max(0L, (long)(ends - _clock.Time).TotalMilliseconds) : 0L;

        return new SocialItemValue
        {
            Actor = placed.Actor,
            Owner = placed.Owner,
            Item = placed.Item,
            SocialItem = (short)placed.SocialItem,
            Remaining = left,
            Users = placed.Users.ToArray()
        };
    }

    private SocialItemPlacedValue PlacedValueOf(SocialObject placed) => new()
    {
        Prop = ValueOf(placed),
        Position = placed.Position,
        Rotation = placed.Rotation
    };

    private static bool IsFinite(Vector3 place)
    {
        return float.IsFinite(place.X) && float.IsFinite(place.Y) && float.IsFinite(place.Z);
    }

    private static bool Within(Vector3 first, Vector3 second)
    {
        return Math.Abs(first.X - second.X) <= Reach && Math.Abs(first.Y - second.Y) <= Reach && Math.Abs(first.Z - second.Z) <= Reach;
    }

    private static bool Near(Person person, SocialObject placed)
    {
        return Math.Abs(person.CellX - placed.CellX) <= 1 && Math.Abs(person.CellY - placed.CellY) <= 1;
    }

    private (int X, int Y) CellOf(Vector3 position)
    {
        return (Index(position.X), Index(position.Y));
    }

    private int Index(float coordinate)
    {
        return (int)Math.Clamp(MathF.Floor(coordinate / _cellSize), -MaxCell, MaxCell);
    }
}
