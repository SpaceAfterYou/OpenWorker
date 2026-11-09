using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;

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
        var options = new byte[LoginModuleDefines.PersonOptionCount];
        options.AsSpan().Fill((byte)'1');
        PersonOptions.AsSpan(0, Math.Min(PersonOptions.Length, options.Length)).CopyTo(options);

        writer.Write(options);
        writer.Write(Contents);
    }
}
