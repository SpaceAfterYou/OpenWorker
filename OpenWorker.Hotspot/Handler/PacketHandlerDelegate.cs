using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// The second shape of a handler, for a request that reads itself through <see cref="PacketReader"/>
/// (<see cref="IWireReadable{TSelf}"/>, generated for a <c>[WireMessage]</c>): it takes the body of the packet after the
/// opcode as a span. The request is parsed whole before the handler runs, because a <see langword="ref struct"/> cannot
/// live across the awaits of the handler; then only the parsed struct goes on.
/// </summary>
internal delegate ValueTask PacketHandlerDelegate(
    IHotspotHandler instance,
    Entity player,
    ReadOnlySpan<byte> body,
    CancellationToken cancellationToken
);

/// <summary>
/// Makes the <see cref="PacketHandlerDelegate"/> of a request type out of the call to the handler that
/// <see cref="HandlerBuilder"/> compiles for it, once, when the table is built.
/// </summary>
internal static class PacketHandler<TRequest> where TRequest : IWireReadable<TRequest>
{
    public static PacketHandlerDelegate Create(Func<IHotspotHandler, Entity, TRequest, CancellationToken, ValueTask> call)
    {
        return (instance, player, body, cancellationToken) =>
        {
            var reader = new PacketReader(body);
            var request = TRequest.Read(ref reader);

            return call(instance, player, request, cancellationToken);
        };
    }
}
