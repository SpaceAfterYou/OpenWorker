using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Types;

/// <summary>
/// A gem the client puts into a socket: where the gem lies and the socket it goes into.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct ItemSetupSocketGem
{
    public StorageSlot Gem { get; init; }
    public byte Socket { get; init; }
}
