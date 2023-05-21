using DefaultEcs;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Handlers;

internal static class EmptyHandler
{
    public static ValueTask OnUnhandledAsync(object _1, Entity _2, BinaryReader _3, CancellationToken _4) => ValueTask.CompletedTask;
}
