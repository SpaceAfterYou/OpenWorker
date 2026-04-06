using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

internal sealed class EmptyHandler : IHotspotHandler
{
    public static ValueTask OnUnhandledAsync(object instance,
        object player,
        BinaryReader reader,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}