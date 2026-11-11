using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
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

public sealed record DropObject(long Serial, int Group, int ObjectId, int Count, int Bonus, Vector3 Position, long ExpiresAt);

/// <summary>
/// Lets monsters drop things and lets players take them. Money and items are objects of their own on the ground,
/// the ids of money are not items.
/// </summary>
public sealed partial class DropService(
    World world,
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
    ReadOnlyCollection<DropRateMemberRow>? memberRates = null)
{
    public const int GoldObject = 800000001;
    public const int SoulObject = 800000002;
    public const int BattlePointObject = 800000003;
    public const int EtherObject = 800000004;

    private const int Chances = 10000;
    private const int BossRank = 4;

    // The tables of a party's drop go up to four members.
    private const int MostMembers = 4;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

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
            var things = Roll(drop, rate, random);

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
    /// Works out what falls: money by its chances, then one item from each group that falls.
    /// </summary>
    public List<(int Object, int Count)> Roll(DropRow drop, float rate, Random random)
    {
        var things = new List<(int, int)>();

        if (drop.GoldChance * rate >= random.Next(1, Chances + 1))
        {
            var multiple = 1;

            if (drop.GoldBonusChance > 0 && drop.GoldBonusMultiple > 0 && random.Next(1, Chances + 1) <= drop.GoldBonusChance)
            {
                multiple = drop.GoldBonusMultiple;
            }

            for (var i = 0; i < multiple; i++)
            {
                things.Add((GoldObject, random.Next(drop.GoldMin, drop.GoldMax + 1)));
            }
        }

        if (drop.BattlePointChance * rate >= random.Next(1, Chances + 1))
        {
            things.Add((BattlePointObject, random.Next(drop.BattlePointMin, drop.BattlePointMax + 1)));
        }

        if (drop.EtherUnit > 0 && drop.EtherChance * rate >= random.Next(1, Chances + 1))
        {
            things.AddRange(RollEther(drop, random));
        }

        for (var i = 0; i < DropRow.GroupCount; i++)
        {
            if (drop.GroupId[i] == 0 || drop.GroupChance[i] * rate < random.Next(1, Chances + 1) || !_groups.TryGetValue(drop.GroupId[i], out var group))
            {
                continue;
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
                    things.Add((group.Item[n], random.Next(group.Min[n], Math.Max(group.Min[n], group.Max[n]) + 1)));
                }

                break;
            }
        }

        return things;
    }

    private static IEnumerable<(int, int)> RollEther(DropRow drop, Random random)
    {
        var parts = Math.Max(1, (drop.EtherMax + drop.EtherUnit - 1) / drop.EtherUnit);
        var least = drop.EtherMin / parts;
        var total = 0;

        for (var i = 0; i < parts && total < drop.EtherMax; i++)
        {
            var given = random.Next(least, drop.EtherUnit + 1);

            total += given;

            yield return (EtherObject, given - Math.Max(0, total - drop.EtherMax));
        }

        if (total < drop.EtherMin)
        {
            yield return (EtherObject, drop.EtherMin - total);
        }
    }

    /// <summary>
    /// Puts things on the ground around a place and tells the player.
    /// </summary>
    public void Place(Entity player, IReadOnlyList<(int Object, int Count)> things, Vector3 position, bool boss = false)
    {
        var field = Field(player);
        var group = Interlocked.Increment(ref _group);
        var expires = Environment.TickCount64 + (long)Lifetime.TotalMilliseconds;
        var values = new List<DropObjectValue>();

        foreach (var (thing, count) in things)
        {
            var spot = position + new Vector3(Random.Shared.Next(-60, 61), Random.Shared.Next(-60, 61), 0);
            var drop = new DropObject(serials.NextSerial(), group, thing, count, 0, spot, expires);

            field.Objects[drop.Serial] = drop;
            values.Add(new DropObjectValue(spot, thing, (short)Math.Min(count, short.MaxValue), 0, drop.Serial));
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
            GoldObject => (added = inventory.AddUpToLimit(player, ShopCurrency.Gold, drop.Count + drop.Bonus, MoneyEarnFlow.PickUp)) >= 0,
            BattlePointObject => inventory.AddUpToLimit(player, ShopCurrency.Bp, drop.Count) >= 0,
            EtherObject => (added = inventory.AddUpToLimit(player, ShopCurrency.Ether, drop.Count)) >= 0,
            SoulObject => true,
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
        }
        else if (drop.ObjectId == EtherObject)
        {
            results?.AddEther(player, (int)added);
            achievements?.OnEtherPicked(player, (int)added);
        }

        // The quests and the missions hear of the item from the inventory, as of every item that comes into the bags.
        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new DropPickUpResponse { Serial = serial });
        }

        return true;
    }

    /// <summary>
    /// Takes away what has lain on the ground too long, a group at a time as the client removes them.
    /// </summary>
    public void Expire()
    {
        var now = Environment.TickCount64;

        foreach (var (player, field) in _field.ToArray())
        {
            if (!world.IsAlive(player))
            {
                _field.Remove(player);
                continue;
            }

            var gone = field.Objects.Values.Where(e => e.ExpiresAt <= now).ToArray();

            foreach (var drop in gone)
            {
                field.Objects.Remove(drop.Serial);
            }

            foreach (var group in gone.Select(e => e.Group).Distinct())
            {
                if (world.Has<ServerSessionComponent>(player))
                {
                    world.Get<ServerSessionComponent>(player).Send(new DropDeleteResponse { DropGroup = group });
                }
            }
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
}
