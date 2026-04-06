using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LoginGateConnectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.ServerConnectReq;

    public short Gate { get; } = reader.ReadInt16();

    public MessageOpcode Opcode => new(Group, Command);
}