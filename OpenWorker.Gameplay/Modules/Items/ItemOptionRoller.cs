using System.Collections.ObjectModel;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What a new item is given when it is made, as the game makes it: its options, rolled from the row of <c>tb_Random_Option</c>
/// its item names, and for a piece of equipment the sockets that are open from the start, some of them by chance. Without the
/// server's own tables an item gets no options and only the sockets the client's table opens.
/// </summary>
public sealed class ItemOptionRoller
{
    private const int Chance = 10_000;

    private readonly RowIndex<int, ItemOptionRollRow> _options;
    private readonly RowIndex<int, SocketRow> _sockets;
    private readonly RowIndex<int, SocketChanceRow> _chances;
    private readonly Func<int, int, int> _roll;

    /// <param name="roll">A number from the first bound to the second, both included; random unless a test fixes it.</param>
    public ItemOptionRoller(
        ReadOnlyCollection<ItemOptionRollRow>? options = null,
        ReadOnlyCollection<SocketRow>? sockets = null,
        ReadOnlyCollection<SocketChanceRow>? chances = null,
        Func<int, int, int>? roll = null)
    {
        _options = (options ?? new ReadOnlyCollection<ItemOptionRollRow>([])).IndexBy(e => e.Id);
        _sockets = (sockets ?? new ReadOnlyCollection<SocketRow>([])).IndexBy(e => e.Id);
        _chances = (chances ?? new ReadOnlyCollection<SocketChanceRow>([])).IndexBy(e => e.Id);
        _roll = roll ?? ((low, high) => Random.Shared.Next(low, high + 1));
    }

    /// <summary>
    /// Gives a new item its options and its open sockets.
    /// </summary>
    public void Roll(ItemInstance item, in ItemRow row, ItemClassifyRow? classify)
    {
        if (row.Option != 0 && _options.TryGet(row.Option, out var options))
        {
            item.Options = RollOptions(options);
        }

        if (row.Socket != 0 && classify is { ItemUseType: ItemUseType.ABILITY })
        {
            item.SocketActive = OpenSockets(row.Socket);
        }
    }

    /// <summary>
    /// A number of options between the bounds of the row, each a different one of the first options it lists, picked at random,
    /// with a value between its bounds. When more options are rolled than the row lists to pick from, the item gets none.
    /// </summary>
    private ItemOption[] RollOptions(in ItemOptionRollRow row)
    {
        var options = new ItemOption[ItemModuleDefines.OptionCount];

        if (row.Max > ItemModuleDefines.OptionCount)
        {
            return options;
        }

        var count = Between(row.Min, row.Max);
        var candidates = Math.Min((int)row.Candidates, ItemOptionRollRow.Options);

        if (count <= 0 || count > candidates)
        {
            return options;
        }

        var order = new int[candidates];

        for (var i = 0; i < candidates; i++)
        {
            order[i] = i;
        }

        for (var i = candidates - 1; i > 0; i--)
        {
            var j = Between(0, i);

            (order[i], order[j]) = (order[j], order[i]);
        }

        for (var i = 0; i < count; i++)
        {
            var pick = order[i];

            options[i] = new ItemOption((short)row.Types[pick], Between(row.Lows[pick], row.Highs[pick]));
        }

        return options;
    }

    /// <summary>
    /// The sockets open from the start, and each extra one by its chance, up to the most the item can have.
    /// </summary>
    private byte OpenSockets(int socket)
    {
        if (_chances.TryGet(socket, out var chances))
        {
            if (chances.FixedSockets + chances.ExtraSockets > chances.MaxSockets || chances.ExtraSockets > SocketChanceRow.Chances)
            {
                return 0;
            }

            var open = chances.FixedSockets;

            for (var extra = 0; extra < chances.ExtraSockets && open < chances.MaxSockets; extra++)
            {
                if (Between(1, Chance) <= chances.ExtraChances[extra])
                {
                    open++;
                }
            }

            return open;
        }

        return _sockets.TryGet(socket, out var row) ? Math.Min(row.FixedSockets, row.MaxSockets) : (byte)0;
    }

    private int Between(int low, int high)
    {
        return low == high ? low : low < high ? _roll(low, high) : _roll(high, low);
    }
}
