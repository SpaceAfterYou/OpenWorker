using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
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

public sealed record DropObject(long Serial, int Group, int Object, int Count, int Bonus, Vector3 Position, long ExpiresAt);

/// <summary>
/// Lets monsters drop things and lets players take them. Money and items are objects of their own on the ground,
/// the ids of money are not items.
/// </summary>
public sealed class DropService(
    World world,
    InventoryService inventory,
    ItemCatalog catalog,
    IItemSerials serials,
    ReadOnlyCollection<DropRow> drops,
    ReadOnlyCollection<DropGroupRow> groups,
    ReadOnlyCollection<DropRateMobRow> rates,
    ReadOnlyCollection<MonsterRow> monsters,
    IConfiguration? configuration = null,
    DailyMissionManager? missions = null,
    ILogger<DropService>? logger = null,
    MazeResults? results = null)
{
    public const int GoldObject = 800000001;
    public const int SoulObject = 800000002;
    public const int BattlePointObject = 800000003;
    public const int EtherObject = 800000004;

    private const int Chances = 10000;
    private const int BossRank = 4;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    // A player takes what is within this reach of its feet. The client decides when to ask, so this only holds back requests from afar.
    private const float PickUpReach = 1500f;

    private readonly Dictionary<int, DropRow> _drops = drops.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<int, DropGroupRow> _groups = groups.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<int, float> _rates = rates.GroupBy(e => (int)e.Interval).ToDictionary(e => e.Key, e => e.First().Rate);
    private readonly Dictionary<int, MonsterRow> _monsters = monsters.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
    private readonly Dictionary<Entity, PlayerDrops> _field = [];

    // How much more or less than the tables say falls, as a server setting.
    private readonly float _rate = configuration?.GetValue("Rewards:DropRate", 1f) ?? 1f;

    private int _group;

    /// <summary>
    /// What a monster lets fall for this player. Nothing happens when the tables do not know the monster.
    /// </summary>
    public void DropForKill(Entity player, int monster, Vector3 position, Random? random = null)
    {
        if (!_monsters.TryGetValue(monster, out var row) || row.DropId == 0 || !_drops.TryGetValue(row.DropId, out var drop))
        {
            return;
        }

        random ??= Random.Shared;

        var level = world.Get<RankComponent>(player).Level;
        var rate = (drop.RateClear != 0 ? 1f : _rates.TryGetValue(Math.Max(0, level - row.Level), out var found) ? found : 1f) * _rate;

        var things = Roll(drop, rate, random);

        if (things.Count == 0)
        {
            return;
        }

        Place(player, things, position, row.Rank == BossRank);
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
            var drop = new DropObject(serials.Next(), group, thing, count, 0, spot, expires);

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

        var taken = drop.Object switch
        {
            GoldObject => inventory.TryAddCurrency(player, ShopCurrency.Gold, drop.Count + drop.Bonus, MoneyEarnFlow.PickUp),
            BattlePointObject => inventory.TryAddCurrency(player, ShopCurrency.Bp, drop.Count),
            EtherObject => inventory.TryAddCurrency(player, ShopCurrency.Ether, drop.Count),
            SoulObject => true,
            _ => inventory.TryGive(player, drop.Object, drop.Count)
        };

        if (!taken)
        {
            return false;
        }

        field.Objects.Remove(serial);

        if (drop.Object == GoldObject)
        {
            results?.AddGold(player, drop.Count + drop.Bonus);
        }
        else if (drop.Object == EtherObject)
        {
            results?.AddEther(player, drop.Count);
        }

        if (drop.Object < GoldObject && missions is not null)
        {
            _ = ReportItemAsync(player, drop);
        }

        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new DropPickUpResponse { Serial = serial });
        }

        return true;
    }

    // The missions that ask for items are told, they are saved and the pickup does not wait for it.
    private async Task ReportItemAsync(Entity player, DropObject drop)
    {
        try
        {
            await missions!.OnItemCollectedAsync(player, drop.Object, (short)Math.Min(drop.Count, short.MaxValue), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger?.LogError(exception, "Counting the item {Item} for the missions failed", drop.Object);
        }
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
