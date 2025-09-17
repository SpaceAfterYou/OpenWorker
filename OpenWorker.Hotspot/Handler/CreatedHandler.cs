using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Handler;

internal readonly record struct CreatedHandler(MessageOpcode Opcode, Type Class, HandlerDelegate Delegate);