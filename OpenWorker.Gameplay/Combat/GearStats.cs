using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What the gear a person wears adds to its stats, as the game has it:
/// <list type="bullet">
/// <item>each piece of equipment its attack and defence, the stat options of the item table and the options rolled for it, the
/// attack and defence of its upgrade level, and the options of the gems in its open sockets, all of it less the share its worn
/// endurance takes away;</item>
/// <item>the sets: a set of which two or more different pieces are worn gives the bonus of every count of pieces from two up to
/// the count worn;</item>
/// <item>the clothing: the stat options of its broaches, and the buff of a full group of three broaches of one theme.</item>
/// </list>
/// The tables beyond the item table are optional; without one its part adds nothing.
/// </summary>
public sealed class GearStats
{
    /// <summary>The upgrade levels that add to the attack and the defence.</summary>
    private const int UpgradeLevels = 10;

    /// <summary>The sets go from two pieces to six.</summary>
    private const int FewestSetPieces = 2;

    private const int MostSetPieces = 6;

    private const int StaticOptions = 5;

    /// <summary>A gem of this sub category gives all of its options to any item.</summary>
    private const byte GemForEveryItem = 1;

    /// <summary>A weapon takes the resistances of a gem, the other gear its attributes.</summary>
    private const byte WeaponSlot = 1;

    private const int FirstAttributeAttack = 80;
    private const int LastAttributeAttack = 85;
    private const int FirstAttributeResistance = 86;
    private const int LastAttributeResistance = 91;

    /// <summary>A broach that counts for a set of broaches.</summary>
    private const short BroachOfASet = 6;

    private const int BroachGroups = 5;
    private const int BroachesPerGroup = 3;

    private const float Percent = 0.01f;

    private readonly ItemCatalog _items;
    private readonly RowIndex<int, ReinforceRow> _reinforce;
    private readonly RowIndex<int, ItemSetItemRow> _sets;
    private readonly RowIndex<int, ItemRepairRow> _repairs;
    private readonly RowIndex<short, BroachSetRow> _broachSets;
    private readonly RowIndex<ushort, BuffRow> _buffs;

    public GearStats(
        ItemCatalog items,
        ReadOnlyCollection<ReinforceRow>? reinforce = null,
        ReadOnlyCollection<ItemSetItemRow>? sets = null,
        ReadOnlyCollection<ItemRepairRow>? repairs = null,
        ReadOnlyCollection<BroachSetRow>? broachSets = null,
        ReadOnlyCollection<BuffRow>? buffs = null)
    {
        _items = items;
        _reinforce = (reinforce ?? Empty<ReinforceRow>()).IndexBy(e => e.Id);
        _sets = (sets ?? Empty<ItemSetItemRow>()).IndexBy(e => e.Id);
        _repairs = (repairs ?? Empty<ItemRepairRow>()).IndexBy(e => e.Id);
        _broachSets = (broachSets ?? Empty<BroachSetRow>()).IndexBy(e => e.Id);
        _buffs = (buffs ?? Empty<BuffRow>()).IndexBy(e => e.Id);
    }

    public ItemCatalog Items => _items;

    /// <summary>
    /// Writes what the worn gear of a person adds into the sheet.
    /// </summary>
    public void Collect(Inventory? inventory, StatSheet sheet)
    {
        if (inventory is null)
        {
            return;
        }

        if (inventory.Find(StorageGroup.AbilityEquip) is { } equipment)
        {
            Dictionary<int, HashSet<int>>? sets = null;

            foreach (var item in equipment.Slots)
            {
                if (item is null || !_items.TryGet(item.Item, out var row) || _items.Classify(item.Item) is not { ItemUseType: ItemUseType.ABILITY } classify)
                {
                    continue;
                }

                CollectEquipment(item, row, classify, sheet);

                if (row.SetItem != 0)
                {
                    sets ??= [];

                    if (!sets.TryGetValue(row.SetItem, out var pieces))
                    {
                        sets.Add(row.SetItem, pieces = []);
                    }

                    pieces.Add(item.Item);
                }
            }

            if (sets is not null)
            {
                foreach (var (set, pieces) in sets)
                {
                    CollectSet(set, pieces, sheet);
                }
            }
        }

        if (inventory.Find(StorageGroup.ShapeEquip) is { } clothing)
        {
            foreach (var item in clothing.Slots)
            {
                if (item is not null && item.Broaches.Length > 0)
                {
                    CollectBroaches(item.Broaches, sheet);
                }
            }
        }
    }

    /// <summary>
    /// The buffs the clothing of a person gives by its broaches: one for each group of three broaches of one theme, as the table
    /// of the sets names it.
    /// </summary>
    public void BroachSetBuffs(Inventory? inventory, List<ushort> buffs)
    {
        if (inventory?.Find(StorageGroup.ShapeEquip) is not { } clothing)
        {
            return;
        }

        foreach (var item in clothing.Slots)
        {
            if (item is not null && item.Broaches.Length > 0)
            {
                for (var group = 0; group < BroachGroups && (group + 1) * BroachesPerGroup <= item.Broaches.Length; group++)
                {
                    if (BroachSetOf(item.Broaches, group) is { } buff && !buffs.Contains((ushort)buff.Id))
                    {
                        buffs.Add((ushort)buff.Id);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The share of its stats a piece of equipment keeps at its endurance: all of it while it is whole, less the penalty of the
    /// repair table for the endurance it has fallen to, nothing at zero.
    /// </summary>
    public float EnduranceShare(in ItemRow row, ItemInstance item)
    {
        if (row.Endurance == 0 || item.Endurance >= row.Endurance || !_repairs.TryGet(item.Endurance, out var repair) || repair.EndurancePenalty <= 0f)
        {
            return 1f;
        }

        return Math.Max(0f, (100f - repair.EndurancePenalty) * Percent);
    }

    private void CollectEquipment(ItemInstance item, in ItemRow row, ItemClassifyRow classify, StatSheet sheet)
    {
        var share = EnduranceShare(row, item);

        sheet.Add(STAT_TYPE.STAT_PATK_MAX, row.PhysicalAttack * share);
        sheet.Add(STAT_TYPE.STAT_MATK_MAX, row.MagicalAttack * share);
        sheet.Add(STAT_TYPE.STAT_PDEF, row.PhysicalDefense * share);
        sheet.Add(STAT_TYPE.STAT_MDEF, row.MagicalDefense * share);

        CollectStaticOptions(row, share, sheet);

        foreach (var option in item.Options)
        {
            if (option.Type != 0)
            {
                sheet.Apply(option.Type, option.Option * share);
            }
        }

        // An upgrade adds its share of the attack and of the defence of the item.
        if (item.Upgrade is > 0 and <= UpgradeLevels && _reinforce.TryGet(row.Reinforce, out var reinforce))
        {
            var rate = reinforce.StatRate(item.Upgrade) * Percent * share;

            sheet.Add(STAT_TYPE.STAT_PATK_MAX, row.PhysicalAttack * rate);
            sheet.Add(STAT_TYPE.STAT_PDEF, row.PhysicalDefense * rate);
        }

        var open = Math.Min(item.SocketActive, item.Sockets.Length);

        for (var place = 0; place < open; place++)
        {
            var socket = item.Sockets[place];

            if (socket.IsEmpty)
            {
                continue;
            }

            CollectGem(socket.Gem, classify.SlotType, share, sheet);

            foreach (var option in socket.Options)
            {
                if (option.Type != 0)
                {
                    sheet.Apply(option.Type, option.Option * share);
                }
            }
        }
    }

    /// <summary>
    /// The stat options of the item table, up to the first empty one. An option of another class works on a skill or a
    /// function, not on a stat.
    /// </summary>
    private static void CollectStaticOptions(in ItemRow row, float share, StatSheet sheet)
    {
        for (var index = 0; index < StaticOptions; index++)
        {
            var (kind, type, value) = row.StatOption(index);

            if (type == 0)
            {
                break;
            }

            if (kind == 0)
            {
                sheet.Apply(type, value * share);
            }
        }
    }

    /// <summary>
    /// The options of a gem: a gem for every item gives them all, any other gives a weapon its resistances to the attributes
    /// and other gear its attacks of the attributes.
    /// </summary>
    private void CollectGem(int gem, byte slot, float share, StatSheet sheet)
    {
        if (!_items.TryGet(gem, out var row) || _items.Classify(gem) is not { } classify)
        {
            return;
        }

        for (var index = 0; index < StaticOptions; index++)
        {
            var (kind, type, value) = row.StatOption(index);

            if (type == 0)
            {
                break;
            }

            var fits = classify.SubCategory == GemForEveryItem || (slot == WeaponSlot
                ? type is >= FirstAttributeResistance and <= LastAttributeResistance
                : type is >= FirstAttributeAttack and <= LastAttributeAttack);

            if (fits && kind == 0)
            {
                sheet.Apply(type, value * share);
            }
        }
    }

    /// <summary>
    /// A set gives the bonus of each count of its pieces from two up to the count of different pieces worn.
    /// </summary>
    private void CollectSet(int set, HashSet<int> worn, StatSheet sheet)
    {
        if (!_sets.TryGet(set, out var row) || row.Count > MostSetPieces)
        {
            return;
        }

        var count = 0;

        for (var piece = 0; piece < row.Count; piece++)
        {
            if (row.Piece(piece) is var item && item != 0 && worn.Contains(item))
            {
                count++;
            }
        }

        for (var pieces = FewestSetPieces; pieces <= count; pieces++)
        {
            foreach (var (kind, type, value) in row.Bonus(pieces))
            {
                if (type != 0 && kind == 0)
                {
                    sheet.Apply(type, value);
                }
            }
        }
    }

    /// <summary>
    /// The stat options of every broach set into the clothing, and the buff of each group of three broaches of one theme: the
    /// stats of that buff, which lasts as long as the clothing is worn.
    /// </summary>
    private void CollectBroaches(int[] broaches, StatSheet sheet)
    {
        foreach (var broach in broaches)
        {
            if (broach > 0 && _items.TryGet(broach, out var row))
            {
                CollectStaticOptions(row, 1f, sheet);
            }
        }

        for (var group = 0; group < BroachGroups && (group + 1) * BroachesPerGroup <= broaches.Length; group++)
        {
            if (BroachSetOf(broaches, group) is { } buff)
            {
                (byte Type, int Value)[] stats = [(buff.Stat1, buff.Value1), (buff.Stat2, buff.Value2), (buff.Stat3, buff.Value3)];

                foreach (var (type, value) in stats)
                {
                    if (type != 0)
                    {
                        sheet.Apply(type, value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The buff of a group of three broaches, when they are all broaches of a set and of one theme and the table names the set
    /// they make: the set is named by the theme, the place of the group and the ranks of the three broaches.
    /// </summary>
    private BuffRow? BroachSetOf(int[] broaches, int group)
    {
        var theme = 0;
        var code = 0;

        for (var place = 0; place < BroachesPerGroup; place++)
        {
            var broach = broaches[group * BroachesPerGroup + place];

            if (broach < 1 || !_items.TryGet(broach, out var row) || row.EffectType != BroachOfASet || (theme != 0 && theme != row.EffectValue))
            {
                return null;
            }

            theme = row.EffectValue;
            code = code * 10 + row.Rank;
        }

        if (theme == 0)
        {
            return null;
        }

        code += 10000 * theme + 1000 * (group + 1);

        return code <= short.MaxValue && _broachSets.TryGet((short)code, out var set) && _buffs.TryGet((ushort)set.Buff, out var buff) ? buff : null;
    }

    private static ReadOnlyCollection<T> Empty<T>() => new([]);
}
