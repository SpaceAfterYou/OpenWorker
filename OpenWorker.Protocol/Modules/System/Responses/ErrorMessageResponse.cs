using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.System.Responses;

/// <summary>
/// Reports a failed request: the opcode of the expected response with the error flag, followed by the code, and by a number
/// the text of the error is filled in with when the error has one (the item a maze asks for). The number is there or not, so
/// the writer is written by hand.
/// </summary>
public readonly struct ErrorMessageResponse(MessageOpcode source, ushort code, uint? parameter = null) : IResponseHotspotMessage, IWireWritable
{
    private const byte ErrorFlag = 0x80;

    public ErrorMessageResponse(MessageOpcode source, PersonErrorCode code) : this(source, (ushort)code)
    {
    }

    public MessageOpcode Opcode => new(source.Group, (byte)(source.Command | ErrorFlag));

    public void Write(ref PacketWriter writer)
    {
        writer.WriteUInt16(code);

        if (parameter is { } value)
        {
            writer.WriteUInt32(value);
        }
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }
}
