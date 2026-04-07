using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

internal delegate ValueTask HandlerDelegate(
    IHotspotHandler instance,
    Entity player,
    BinaryReader reader,
    CancellationToken cancellationToken
);
