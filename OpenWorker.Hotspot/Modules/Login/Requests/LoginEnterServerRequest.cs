using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Requests;

[HotspotMessage(Group, Command)]
public readonly struct LoginEnterServerRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.EnterServerReq;

    public int Account { get; } = reader.ReadInt32();
    public short Gate { get; } = reader.ReadInt16();
    public SessionValue Session { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}