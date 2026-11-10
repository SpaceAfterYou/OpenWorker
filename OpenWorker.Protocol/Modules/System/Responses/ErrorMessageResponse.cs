using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Modules.System.Responses;

/// <summary>
/// Reports a failed request: the opcode of the expected response with the error flag, followed by the code, and by a number
/// the text of the error is filled in with when the error has one (the item a maze asks for).
/// </summary>
public readonly struct ErrorMessageResponse(MessageOpcode source, ushort code, uint? parameter = null) : IResponseHotspotMessage
{
    private const byte ErrorFlag = 0x80;

    public ErrorMessageResponse(MessageOpcode source, PersonErrorCode code) : this(source, (ushort)code)
    {
    }

    public MessageOpcode Opcode => new(source.Group, (byte)(source.Command | ErrorFlag));

    public void Write(BinaryWriter writer)
    {
        writer.Write(code);

        if (parameter is { } value)
        {
            writer.Write(value);
        }
    }
}
