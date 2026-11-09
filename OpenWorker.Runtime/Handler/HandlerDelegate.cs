using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// A handler of a request that reads itself from a <see cref="BinaryReader"/> (its constructor). The requests that read
/// themselves through <c>PacketReader</c> have <see cref="PacketHandlerDelegate"/> instead, which the executor prefers; they
/// keep this one too, over the bridge constructor the generator writes, until every request has moved.
/// </summary>
internal delegate ValueTask HandlerDelegate(
    IHotspotHandler instance,
    Entity player,
    BinaryReader reader,
    CancellationToken cancellationToken
);
