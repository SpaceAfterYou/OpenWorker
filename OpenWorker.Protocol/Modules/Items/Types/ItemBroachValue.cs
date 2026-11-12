using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// The broaches set into the gear with this serial, as a letter carries them: always <see cref="ItemCount"/> items.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct ItemBroachValue : IWritableData
{
    public const int ItemCount = 15;

    public long Serial { get; init; }

    [WireArray(Count = ItemCount)]
    public int[] Items { get; init; }
}
