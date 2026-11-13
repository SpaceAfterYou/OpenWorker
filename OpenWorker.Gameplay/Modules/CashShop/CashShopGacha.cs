using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// What gacha items give. A gacha item names its gacha (<c>tb_Random_Get_Group</c>, by the value of its effect); each one
/// opened rolls a group by the chances of <c>tb_Gacha_Group</c>, then an item of that group's draw (<c>tb_Random_Get</c>),
/// out of ten thousand both. The three tables are the server's own; without them no gacha opens.
/// </summary>
public sealed class CashShopGacha
{
    /// <summary>
    /// The effect of an item that is a gacha.
    /// </summary>
    public const short GachaEffect = 5;

    /// <summary>
    /// The most of one gacha item opened at once.
    /// </summary>
    public const int MaxCount = 10;

    private const int Whole = 10_000;

    private readonly RowIndex<int, GachaGroupRow> _chances;
    private readonly RowIndex<int, GachaRow> _gachas;
    private readonly RowIndex<int, GachaDrawRow> _draws;
    private readonly ItemCatalog _items;
    private readonly Func<int, int, int> _roll;

    public CashShopGacha(
        ReadOnlyCollection<GachaGroupRow> chances,
        ReadOnlyCollection<GachaRow> gachas,
        ReadOnlyCollection<GachaDrawRow> draws,
        ItemCatalog items,
        Func<int, int, int>? roll = null)
    {
        _chances = chances.IndexBy(e => (int)e.Group);
        _gachas = gachas.IndexBy(e => (int)e.Id);
        _draws = draws.IndexBy(e => (int)e.Id);
        _items = items;
        _roll = roll ?? ((low, high) => Random.Shared.Next(low, high + 1));
    }

    /// <summary>
    /// The gacha an item opens, when it is a gacha item.
    /// </summary>
    public bool TryGetGacha(int item, out GachaRow gacha)
    {
        gacha = default;

        return _items.TryGet(item, out var row) && row.EffectType == GachaEffect && _gachas.TryGet(row.EffectValue, out gacha);
    }

    /// <summary>
    /// Whether the gacha may be opened on <paramref name="today"/>: one without a period always may.
    /// </summary>
    public static bool Open(GachaRow gacha, DateTime today)
    {
        return !gacha.Period ||
               today.Year >= gacha.StartYear && today.Year <= gacha.EndYear &&
               today.Month >= gacha.StartMonth && today.Month <= gacha.EndMonth &&
               today.Day >= gacha.StartDay && today.Day <= gacha.EndDay;
    }

    /// <summary>
    /// Opens the gacha <paramref name="count"/> times: one item each. A group without its draw and an item the client does not
    /// know are left out of the roll, which runs over the chances left. Nothing when nothing is left to roll.
    /// </summary>
    public List<GachaResultValue>? Draw(GachaRow gacha, int count)
    {
        if (!_chances.TryGet(gacha.Chances, out var chances))
        {
            return null;
        }

        var groups = new int[GachaGroupRow.Groups];

        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = _draws.TryGet(gacha.Draws[index], out _) ? chances.Chances[index] : 0;
        }

        var results = new List<GachaResultValue>(count);

        for (var time = 0; time < count; time++)
        {
            var group = Pick(groups);

            if (group < 0 || !_draws.TryGet(gacha.Draws[group], out var draw))
            {
                return null;
            }

            var known = new int[GachaDrawRow.Items];

            for (var index = 0; index < known.Length; index++)
            {
                known[index] = _items.Exists(draw.ItemList[index]) ? draw.Chances[index] : 0;
            }

            var place = Pick(known);

            if (place < 0)
            {
                return null;
            }

            results.Add(new GachaResultValue { Item = draw.ItemList[place], Type = draw.Types[place] });
        }

        return results;
    }

    /// <summary>
    /// The place a roll falls on, the chances taken in order: out of ten thousand while they make that much, otherwise out of
    /// what they make; -1 when they make nothing.
    /// </summary>
    private int Pick(int[] chances)
    {
        var total = chances.Sum();

        if (total <= 0)
        {
            return -1;
        }

        var point = _roll(1, Math.Min(total, Whole));

        for (var index = 0; index < chances.Length; index++)
        {
            point -= chances[index];

            if (point <= 0 && chances[index] > 0)
            {
                return index;
            }
        }

        return -1;
    }
}
