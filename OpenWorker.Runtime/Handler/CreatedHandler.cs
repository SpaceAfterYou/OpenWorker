using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Handler;

/// <param name="Packet">The handler over the span of the packet.</param>
internal readonly record struct CreatedHandler(MessageOpcode Opcode, Type Class, PacketHandlerDelegate Packet);