using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LoginEnterGateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.EnterServerRes;

    public MessageOpcode Opcode => new(Group, Command);

    public bool HasError { get; init; }
    public int Account { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(HasError);
        writer.Write(HasError ? -1 : Account);
    }

    public static LoginEnterGateResponse Error => new() { HasError = true, Account = -1 };
}
