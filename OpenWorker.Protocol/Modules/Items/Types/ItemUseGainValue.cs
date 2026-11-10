using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// What using an item gave the person, for the window that shows it: experience, gold, battle points and ether, two titles,
/// a helper and the items.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ItemUseGainValue
{
    public int Experience { get; init; }

    public int Gold { get; init; }

    public int BattlePoint { get; init; }

    public int Ether { get; init; }

    public int Title { get; init; }

    public int SecondTitle { get; init; }

    public int Helper { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<ItemUseGainItem> Items { get; init; }
}

/// <summary>
/// An item an opened box gave, and how many.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ItemUseGainItem
{
    public int Item { get; init; }

    public short Count { get; init; }
}
