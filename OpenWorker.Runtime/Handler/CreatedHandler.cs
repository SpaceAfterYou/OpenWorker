using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Handler;

/// <param name="Packet">The handler over the span of the packet, for a request with a span format; <see langword="null"/>
/// for the others.</param>
internal readonly record struct CreatedHandler(MessageOpcode Opcode, Type Class, HandlerDelegate Delegate, PacketHandlerDelegate? Packet = null);