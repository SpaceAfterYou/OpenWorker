using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

/// <summary>
/// The options of the account and the contents the server offers. The options always go as
/// <see cref="LoginModuleDefines.PersonOptionCount"/> bytes, the missing ones as '1', so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LoginOptionLoadResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.OptionLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required byte[] PersonOptions { get; init; }
    public required ServerContent Contents { get; init; }

    public void Write(ref PacketWriter writer)
    {
        Span<byte> options = stackalloc byte[LoginModuleDefines.PersonOptionCount];
        options.Fill((byte)'1');

        var given = PersonOptions.AsSpan();
        given[..Math.Min(given.Length, options.Length)].CopyTo(options);

        writer.WriteBytes(options);
        Contents.Write(ref writer);
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
