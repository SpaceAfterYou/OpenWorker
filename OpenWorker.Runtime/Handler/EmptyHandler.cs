using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// What every opcode without a handler runs: nothing. The packet is not read.
/// </summary>
internal sealed class EmptyHandler : IHotspotHandler
{
    public static ValueTask OnUnhandledAsync(IHotspotHandler instance,
        Entity player,
        ReadOnlySpan<byte> body,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
