using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A gem in a socket of the gear with this serial, and its options, as a letter carries it.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct ItemSocketValue
{
    public long EquipSerial { get; init; }
    public int Socket { get; init; }
    public byte Position { get; init; }

    [WireArray(Count = ItemModuleDefines.OptionCount)]
    public List<ItemOption> Options { get; init; }
}
