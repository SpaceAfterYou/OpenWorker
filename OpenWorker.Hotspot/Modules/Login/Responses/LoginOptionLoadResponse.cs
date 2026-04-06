using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LoginOptionLoadResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.OptionLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public required byte[] PersonOptions { get; init; }
    public required ServerContent Contents { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(PersonOptions);
        writer.Write(Contents);
    }
}
