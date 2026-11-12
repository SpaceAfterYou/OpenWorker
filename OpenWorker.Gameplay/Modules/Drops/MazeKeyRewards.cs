using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// The second reward of the window shown at the end of a maze: an item rolled from the random box the maze names, which the
/// player takes by spending keys (items of the kind <see cref="ItemUseType.REWARD_ADD"/>). It is rolled once when the maze is
/// finished and taken at most once.
/// </summary>
/// <remarks>Runs on the world loop, like the handlers that call it.</remarks>
public sealed partial class MazeKeyRewards(
    InventoryService inventory,
    ReadOnlyCollection<MazeRewardItemRow> rewards,
    ReadOnlyCollection<ItemRandomBoxRow> boxes,
    ILogger<MazeKeyRewards> logger)
{
    /// <summary>Nothing waits to be taken, or the slot holds no key, or not enough of them.</summary>
    public const ushort Refused = InventoryService.Refused;

    /// <summary>The reward does not fit the bags; it waits until it does.</summary>
    public const ushort DoesNotFit = 52013;

    /// <summary>
    /// The item and the count the window shows, and how many keys take it.
    /// </summary>
    public readonly record struct Offer(int Item, short Count, int Keys);

    private static readonly StorageGroup[] Bags = [StorageGroup.Common, StorageGroup.Costume, StorageGroup.Cash, StorageGroup.Cube];

    private readonly RowIndex<uint, MazeRewardItemRow> _rewards = rewards.IndexBy(e => e.Maze);
    private readonly RowIndex<int, ItemRandomBoxRow> _boxes = boxes.IndexBy(e => e.Id);
    private readonly Dictionary<Entity, Offer> _offers = [];

    /// <summary>
    /// Rolls the reward of a finished maze for the player and keeps it until it is taken or the player leaves. A maze
    /// without a key reward, or whose box gives no item, offers nothing.
    /// </summary>
    public Offer? Roll(Entity player, int maze)
    {
        _offers.Remove(player);

        if (maze <= 0 || !_rewards.TryGet((uint)maze, out var row) || row.Keys == 0 || row.KeyBox is 0 or > int.MaxValue ||
            !_boxes.TryGet((int)row.KeyBox, out var box) || Pick(box) is not { } picked ||
            !inventory.Catalog.Exists(picked.Item))
        {
            return null;
        }

        var offer = new Offer(picked.Item, picked.Count, row.Keys);

        _offers[player] = offer;
        LogRolled(logger, maze, offer.Item, offer.Count, offer.Keys);

        return offer;
    }

    /// <summary>
    /// The player takes the reward with the keys of a slot of its bags: the keys are spent and the item given, both or
    /// neither. <see langword="null"/> when it is taken, else the code of the refusal.
    /// </summary>
    public ushort? Take(Entity player, StorageGroup storage, short slot)
    {
        if (!_offers.TryGetValue(player, out var offer))
        {
            return Refused;
        }

        var key = Bags.Contains(storage) ? inventory.InventoryOf(player)?.At(storage, slot) : null;

        if (key is null || !IsKey(key.Item) || key.Count < offer.Keys)
        {
            return Refused;
        }

        // The reward is given before the keys are used: a refused gift takes nothing.
        if (!inventory.TryGive(player, offer.Item, offer.Count))
        {
            return DoesNotFit;
        }

        inventory.Spend(player, key, offer.Keys);

        _offers.Remove(player);
        LogTaken(logger, offer.Item, offer.Count, offer.Keys);

        return null;
    }

    public void Forget(Entity player)
    {
        _offers.Remove(player);
    }

    /// <summary>
    /// The rates of the buckets are rolled against a total that grows with the rank: a rank reaches its own bucket and the
    /// ones below it (the rates of almost every row add up to these totals, bucket by bucket).
    /// </summary>
    private static readonly int[] RankRollTotals = [10, 40, 160, 640, 2560, 10240];

    /// <summary>
    /// The item of the rank of a finished maze: a bucket by its rate, of those the rank reaches, then one item of the box
    /// of that bucket by the rates of its items. Null when the maze has no row, the bucket no box, or the box no item the
    /// item tables know.
    /// </summary>
    public (int Item, short Count)? RollForRank(int maze, byte rank)
    {
        if (maze <= 0 || rank >= RankRollTotals.Length || !_rewards.TryGet((uint)maze, out var row))
        {
            return null;
        }

        var point = Random.Shared.Next(RankRollTotals[rank] + 1);

        for (var bucket = 0; bucket < MazeRewardItemRow.RankCount; bucket++)
        {
            if (point > row.RankRates[bucket])
            {
                point -= row.RankRates[bucket];
                continue;
            }

            if (row.RankBoxes[bucket] is 0 or > int.MaxValue || !_boxes.TryGet((int)row.RankBoxes[bucket], out var box) ||
                Pick(box) is not { } picked || !inventory.Catalog.Exists(picked.Item))
            {
                return null;
            }

            LogRankRolled(logger, maze, rank, bucket, picked.Item, picked.Count);

            return picked;
        }

        return null;
    }

    private bool IsKey(int item)
    {
        return inventory.Catalog.Classify(item)?.ItemUseType == ItemUseType.REWARD_ADD;
    }

    /// <summary>
    /// One item of the box by the rates of its items; the money the box may also hold is not part of this reward.
    /// </summary>
    private static (int Item, short Count)? Pick(ItemRandomBoxRow box)
    {
        (short Rate, int Item, int Count)[] slots =
        [
            (box.ItemRate1, box.Item1, box.Count1), (box.ItemRate2, box.Item2, box.Count2), (box.ItemRate3, box.Item3, box.Count3),
            (box.ItemRate4, box.Item4, box.Count4), (box.ItemRate5, box.Item5, box.Count5), (box.ItemRate6, box.Item6, box.Count6),
            (box.ItemRate7, box.Item7, box.Count7), (box.ItemRate8, box.Item8, box.Count8), (box.ItemRate9, box.Item9, box.Count9),
            (box.ItemRate10, box.Item10, box.Count10), (box.ItemRate11, box.Item11, box.Count11), (box.ItemRate12, box.Item12, box.Count12),
            (box.ItemRate13, box.Item13, box.Count13), (box.ItemRate14, box.Item14, box.Count14), (box.ItemRate15, box.Item15, box.Count15)
        ];

        var candidates = slots.Where(e => e.Rate > 0 && e.Item > 0 && e.Count is > 0 and <= short.MaxValue).ToArray();
        var point = candidates.Sum(e => (int)e.Rate);

        if (point <= 0)
        {
            return null;
        }

        point = Random.Shared.Next(point);

        foreach (var (rate, item, count) in candidates)
        {
            if (point < rate)
            {
                return (item, (short)count);
            }

            point -= rate;
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Maze {Maze} offers {Count} of item {Item} for {Keys} keys")]
    private static partial void LogRolled(ILogger logger, int maze, int item, short count, int keys);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Maze {Maze} at rank {Rank} gives bucket {Bucket}: {Count} of item {Item}")]
    private static partial void LogRankRolled(ILogger logger, int maze, byte rank, int bucket, int item, short count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Key reward taken: {Count} of item {Item} for {Keys} keys")]
    private static partial void LogTaken(ILogger logger, int item, short count, int keys);
}
