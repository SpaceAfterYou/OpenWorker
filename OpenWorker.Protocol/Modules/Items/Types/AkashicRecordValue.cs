using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// A card kept in the akashic record book of a person: the card and its place in the book.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct AkashicRecordValue
{
    public int Card { get; init; }

    public int Position { get; init; }
}
