using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Messages.Abstractions;

public interface IHotspotMessage
{
    MessageOpcode Opcode { get; }
}