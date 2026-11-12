using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

/// <summary>
/// The answer to entering a gate: an error flag, then the account, which is -1 whenever the flag is set. The account depends
/// on the flag, so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LoginEnterGateResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.EnterServerRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public bool HasError { get; init; }
    public int Account { get; init; }

    public void Write(ref PacketWriter writer)
    {
        writer.WriteBoolean(HasError);
        writer.WriteInt32(HasError ? -1 : Account);
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }

    public static LoginEnterGateResponse Error => new() { HasError = true, Account = -1 };
}
