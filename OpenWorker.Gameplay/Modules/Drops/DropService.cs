using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Drops;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// What lies on the ground for one player: it is theirs alone, nobody else sees it or can take it.
/// </summary>
public sealed class PlayerDrops
{
    public Dictionary<long, DropObject> Objects { get; } = [];
}

/// <summary>
/// Something that falls: an item or a kind of money, how many of it in all, and how many of those a booster added.
/// </summary>
public readonly record struct DropThing(int ObjectId, int Count, int Bonus = 0);

public sealed record DropObject(long Serial, int Group, int ObjectId, int Count, int Bonus, Vector3 Position, TimeSpan ExpiresAt);

/// <summary>
/// Lets monsters drop things and lets players take them. Money and items are objects of their own on the ground,
/// the ids of money are not items. What falls together lies on the ground for <see cref="Lifetime"/> of game time, then
/// goes away and the client is told; a stalled maze does not take it away sooner.
/// </summary>
public sealed partial class DropService(
    World world,
    SimulationClock clock,
    InventoryService inventory,
    ItemCatalog catalog,
    IItemSerials serials,
    ReadOnlyCollection<DropRow> drops,
    ReadOnlyCollection<DropGroupRow> groups,
    ReadOnlyCollection<DropRateMobRow> rates,
    ReadOnlyCollection<MonsterRow> monsters,
    IOptions<RewardsOptions>? options = null,
    MazeResults? results = null,
    AchievementManager? achievements = null,
    ReadOnlyCollection<DropRateLevelRow>? levelRates = null,
    ReadOnlyCollection<DropRateMemberRow>? memberRates = null,
    BoosterManager? boosters = null,
    ItemUseService? use = null,
    HiddenEventCounter? hiddenEvents = null,
    ILogger<DropService>? logger = null)
{
    public const int GoldObject = 800000001;
    public const int SoulObject = 800000002;
    public const int BattlePointObject = 800000003;
    public const int EtherObject = 800000004;

    private const int Chances = 10000;
    private const int BossRank = 4;

    // The tables of a party's drop go up to four members.
    private const int MostMembers = 4;

    /// <summary>
    /// How long what falls together lies on the ground.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(3);

    // A player takes what is within this reach of its feet. The client decides when to ask, so this only holds back requests from afar.
    private const float PickUpReach = 1500f;

    private readonly Dictionary<int, DropRow> _drops = drops.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<int, DropGroupRow> _groups = groups.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<int, float> _rates = rates.GroupBy(e => (int)e.Interval).ToDictionary(e => e.Key, e => e.First().Rate);
    private readonly Dictionary<int, MonsterRow> _monsters = monsters.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<Entity, PlayerDrops> _field = [];

    // What a party's drop is multiplied by: the gap of the levels of its members, and how many members share the kill.
    private readonly Dictionary<int, float> _levelRates = (levelRates ?? new ReadOnlyCollection<DropRateLevelRow>([])).GroupBy(e => (int)e.Interval).ToDictionary(e => e.Key, e => e.First().Rate);
    private readonly Dictionary<int, float> _memberRates = (memberRates ?? new ReadOnlyCollection<DropRateMemberRow>([])).GroupBy(e => (int)e.Members).ToDictionary(e => e.Key, e => e.First().Rate);

    // How much more or less than the tables say falls, as a server setting.
    private readonly float _rate = options?.Value.DropRate ?? 1f;

    private int _group;

    private readonly ILogger _logger = logger ?? NullLogger<DropService>.Instance;

    // The rows whose pair of values was found the wrong way round, told once each.
    private readonly HashSet<(int Row, string Pair)> _reversed = [];

    /// <summary>
    /// What a monster lets fall for this player. Nothing happens when the tables do not know the monster.
    /// </summary>
    public void DropForKill(Entity player, int monster, Vector3 position, Random? random = null)
    {
        DropForKill([player], monster, position, random);
    }

    /// <summary>
    /// What a monster lets fall for the players of its maze. Each of them gets a drop of their own, rolled by itself, that
    /// nobody else sees or can take; a party's drop is multiplied by how much the levels of its members differ and by how
    /// many of them share the kill. Nothing happens when the tables do not know the monster.
    /// </summary>
    public void DropForKill(IReadOnlyList<Entity> players, int monster, Vector3 position, Random? random = null)
    {
        if (!_monsters.TryGetValue(monster, out var row) || row.DropId == 0 || !_drops.TryGetValue(row.DropId, out var drop))
        {
            return;
        }

        random ??= Random.Shared;

        var members = players.Where(world.IsAlive).ToArray();

        if (members.Length == 0)
        {
            return;
        }

        var rate = PartyRate(members, row, drop) * _rate;

        foreach (var member in members)
        {
            var things = Roll(drop, rate, random, member);

            if (things.Count > 0)
            {
                Place(member, things, position, row.Rank == BossRank);
            }
        }
    }

    /// <summary>
    /// How much of the drop each of the players gets: for a party, the rate of the gap between its highest and lowest level
    /// and of its size; then, unless the drop is exempt, the rate of how far its highest level is above the monster. The
    /// original multiplies the last rate again for every member after the first, which looks like a mistake and is not
    /// copied.
    /// </summary>
    private float PartyRate(Entity[] members, in MonsterRow monster, in DropRow drop)
    {
        var levels = members.Select(e => (int)world.Get<LevelComponent>(e).Level).ToArray();
        var highest = levels.Max();
        var rate = 1f;

        if (members.Length > 1)
        {
            rate *= _levelRates.GetValueOrDefault(highest - levels.Min(), 1f) * _memberRates.GetValueOrDefault(Math.Min(members.Length, MostMembers), 1f);
        }

        return drop.RateClear != 0 ? rate : rate * _rates.GetValueOrDefault(Math.Max(0, highest - monster.Level), 1f);
    }

    /// <summary>
    /// A chance with what the boosters of the player add to it: the chance, a part of it for the rates, and the values.
    /// </summary>
    private float Boosted(Entity? player, BoosterEffect effect, float chance)
    {
        return player is { } known && boosters is not null ? boosters.Apply(known, effect, chance) : chance;
    }

    /// <summary>
    /// Works out what falls: money by its chances, then one item from each group that falls. The boosters of the player
    /// add to the gold that falls and to the chances of battle points, ether and the items of each group.
    /// </summary>
    public List<DropThing> Roll(DropRow drop, float rate, Random random, Entity? player = null)
    {
        var things = new List<DropThing>();

        // Each part is rolled by itself: a part that fails on a bad row does not take the others with it.
        Part(drop.Id, "gold", () => RollGold(drop, rate, random, player, things));

        Part(drop.Id, "battle points", () =>
        {
            if (Boosted(player, BoosterEffect.IncBattlePoint, drop.BattlePointChance * rate) >= random.Next(1, Chances + 1))
            {
                things.Add(new DropThing(BattlePointObject, Between(random, drop.BattlePointMin, drop.BattlePointMax, drop.Id, "battle points")));
            }
        });

        Part(drop.Id, "ether", () =>
        {
            if (drop.EtherUnit > 0 && Boosted(player, BoosterEffect.IncEtherDrop, drop.EtherChance * rate) >= random.Next(1, Chances + 1))
            {
                things.AddRange(RollEther(drop, random));
            }
        });

        for (var i = 0; i < DropRow.GroupCount; i++)
        {
            var index = i;

            Part(drop.Id, "item group", () => RollGroup(drop, index, rate, random, player, things));
        }

        return things;
    }

    private void Part(int row, string part, Action roll)
    {
        try
        {
            roll();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogPartFailed(_logger, e, part, row);
        }
    }

    private void RollGold(in DropRow drop, float rate, Random random, Entity? player, List<DropThing> things)
    {
        if (drop.GoldChance * rate < random.Next(1, Chances + 1))
        {
            return;
        }

        var multiple = 1;

        if (drop.GoldBonusChance > 0 && drop.GoldBonusMultiple > 0 && random.Next(1, Chances + 1) <= drop.GoldBonusChance)
        {
            multiple = drop.GoldBonusMultiple;
        }

        var goldRate = player is { } known && boosters is not null ? boosters.GetRate(known, BoosterEffect.IncGoldDrop) : 0f;

        for (var i = 0; i < multiple; i++)
        {
            var gold = Between(random, drop.GoldMin, drop.GoldMax, drop.Id, "gold");
            var bonus = (int)(gold * goldRate);

            things.Add(new DropThing(GoldObject, gold + bonus, bonus));
        }
    }

    private void RollGroup(in DropRow drop, int index, float rate, Random random, Entity? player, List<DropThing> things)
    {
        if (drop.GroupId[index] == 0 || Boosted(player, BoosterEffect.IncItemDrop, drop.GroupChance[index] * rate) < random.Next(1, Chances + 1) || !_groups.TryGetValue(drop.GroupId[index], out var group))
        {
            return;
        }

        var point = random.Next(1, Chances + 1);

        for (var n = 0; n < DropGroupRow.ItemCount; n++)
        {
            point -= group.Chance[n];

            if (point > 0)
            {
                continue;
            }

            // Items the client does not have are left lying in the table.
            if (group.Item[n] > 0 && catalog.Exists(group.Item[n]))
            {
                things.Add(new DropThing(group.Item[n], Between(random, group.Min[n], group.Max[n], group.Id, "item count")));
            }

            break;
        }
    }

    /// <summary>
    /// A value between the two of a pair of a table, both included. Some rows have the pair the wrong way round: it is
    /// rolled between its two values all the same, and told once for the row.
    /// </summary>
    private int Between(Random random, int least, int most, int row, string pair)
    {
        Order(ref least, ref most, row, pair);

        return random.Next(least, most + 1);
    }

    private void Order(ref int least, ref int most, int row, string pair)
    {
        if (least <= most)
        {
            return;
        }

        if (_reversed.Add((row, pair)))
        {
            LogReversed(_logger, pair, row, least, most);
        }

        (least, most) = (most, least);
    }

    private IEnumerable<DropThing> RollEther(DropRow drop, Random random)
    {
        var smallest = (int)drop.EtherMin;
        var largest = (int)drop.EtherMax;

        Order(ref smallest, ref largest, drop.Id, "ether");

        var parts = Math.Max(1, (largest + drop.EtherUnit - 1) / drop.EtherUnit);
        var least = smallest / parts;
        var total = 0;

        for (var i = 0; i < parts && total < largest; i++)
        {
            var given = random.Next(least, drop.EtherUnit + 1);

            total += given;

            yield return new DropThing(EtherObject, given - Math.Max(0, total - largest));
        }

        if (total < smallest)
        {
            yield return new DropThing(EtherObject, smallest - total);
        }
    }

    /// <summary>
    /// Puts things on the ground around a place and tells the player.
    /// </summary>
    public void Place(Entity player, IReadOnlyList<DropThing> things, Vector3 position, bool boss = false)
    {
        var field = Field(player);
        var group = Interlocked.Increment(ref _group);
        var expires = clock.Now + Lifetime;
        var values = new List<DropObjectValue>();

        foreach (var (thing, count, bonus) in things)
        {
            // A drop is made in the middle of a simulation step, which cannot wait: with no serial ready the thing is not dropped.
            if (!serials.TryNextSerial(out var serial))
            {
                continue;
            }

            var spot = position + new Vector3(Random.Shared.Next(-60, 61), Random.Shared.Next(-60, 61), 0);
            var drop = new DropObject(serial, group, thing, count, bonus, spot, expires);

            field.Objects[drop.Serial] = drop;
            values.Add(new DropObjectValue(spot, thing, (short)Math.Min(count, short.MaxValue), (short)Math.Min(bonus, short.MaxValue), drop.Serial));
        }

        if (values.Count > 0)
        {
            clock.Schedule(Lifetime, () => Disappear(player, field, group));
        }

        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new DropInfosResponse { DropGroup = group, Boss = boss, Objects = values });
        }
    }

    public IReadOnlyCollection<DropObject> On(Entity player)
    {
        return _field.TryGetValue(player, out var field) ? field.Objects.Values : [];
    }

    /// <summary>
    /// A player takes a thing from the ground. It stays there when the bag has no room for it.
    /// </summary>
    public bool PickUp(Entity player, long serial)
    {
        if (!_field.TryGetValue(player, out var field) || !field.Objects.TryGetValue(serial, out var drop))
        {
            return false;
        }

        if (Vector3.Distance(world.Get<WorldComponent>(player).Position, drop.Position) > PickUpReach)
        {
            return false;
        }

        // Money fills the purse up to its limit and the rest is lost; items that do not fit stay on the ground.
        var added = 0L;

        var taken = drop.ObjectId switch
        {
            GoldObject => (added = inventory.AddUpToLimit(player, ShopCurrency.Gold, drop.Count, MoneyEarnFlow.PickUp)) >= 0,
            BattlePointObject => inventory.AddUpToLimit(player, ShopCurrency.Bp, drop.Count) >= 0,
            EtherObject => (added = inventory.AddUpToLimit(player, ShopCurrency.Ether, drop.Count)) >= 0,
            SoulObject => true,
            // A fever item acts at once and does not go into the bags.
            _ when use?.UseOnPickUp(player, drop.ObjectId) == true => true,
            _ => inventory.TryGive(player, drop.ObjectId, drop.Count)
        };

        if (!taken)
        {
            return false;
        }

        field.Objects.Remove(serial);

        if (drop.ObjectId == GoldObject)
        {
            results?.AddGold(player, (int)added);
            achievements?.OnGoldPicked(player, (int)added);
            hiddenEvents?.Count(player, HiddenEventTarget.Gold, drop.ObjectId, (int)added);
        }
        else if (drop.ObjectId == EtherObject)
        {
            results?.AddEther(player, (int)added);
            achievements?.OnEtherPicked(player, (int)added);
            hiddenEvents?.Count(player, HiddenEventTarget.Ether, drop.ObjectId, (int)added);
        }
        else if (drop.ObjectId == SoulObject)
        {
            hiddenEvents?.Count(player, HiddenEventTarget.Soul, drop.ObjectId, drop.Count);
        }

        // The quests and the missions hear of the item from the inventory, as of every item that comes into the bags.
        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new DropPickUpResponse { Serial = serial });
        }

        return true;
    }

    /// <summary>
    /// What fell together has lain on the ground its time: what is left of it goes away, and the client is told. Nothing
    /// happens for a player who has left, or whose ground was cleared since.
    /// </summary>
    private void Disappear(Entity player, PlayerDrops field, int group)
    {
        if (!world.IsAlive(player) || !_field.TryGetValue(player, out var current) || current != field)
        {
            return;
        }

        var gone = field.Objects.Values.Where(e => e.Group == group).Select(e => e.Serial).ToArray();

        if (gone.Length == 0)
        {
            return;
        }

        foreach (var serial in gone)
        {
            field.Objects.Remove(serial);
        }

        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new DropDeleteResponse { DropGroup = group });
        }
    }

    public void Forget(Entity player)
    {
        _field.Remove(player);
    }

    private PlayerDrops Field(Entity player)
    {
        if (!_field.TryGetValue(player, out var field))
        {
            _field[player] = field = new PlayerDrops();
        }

        return field;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Row {Row} of the drop tables has its {Pair} the wrong way round ({First} before {Second}), rolled between the two")]
    private static partial void LogReversed(ILogger logger, string pair, int row, int first, int second);

    [LoggerMessage(Level = LogLevel.Error, Message = "The {Part} of drop row {Row} could not be rolled, the rest of the drop falls")]
    private static partial void LogPartFailed(ILogger logger, Exception exception, string part, int row);
}
