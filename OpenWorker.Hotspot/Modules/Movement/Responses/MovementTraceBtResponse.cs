using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementTraceBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.TraceBt;

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
    }
}