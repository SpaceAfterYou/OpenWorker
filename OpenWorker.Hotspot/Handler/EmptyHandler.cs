using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

internal sealed class EmptyHandler : IHotspotHandler
{
    public static ValueTask OnUnhandledAsync(object instance,
        Entity player,
        BinaryReader reader,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
