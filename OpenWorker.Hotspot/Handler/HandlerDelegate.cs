using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

internal delegate ValueTask HandlerDelegate(
    IHotspotHandler instance,
    object player,
    BinaryReader reader,
    CancellationToken cancellationToken
);