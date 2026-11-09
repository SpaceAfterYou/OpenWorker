using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Types;

/// <summary>
/// What one item gave when it was taken apart: the item taken apart, the item it gave and how many (zero when it gave no
/// item), and the ether it gave. The client writes a line in the chat for each.
/// </summary>
[WireMessage]
public readonly partial struct ItemSetupDisassembleResult
{
    public int Item { get; init; }

    public int Result { get; init; }

    public short Count { get; init; }

    public int Ether { get; init; }
}
