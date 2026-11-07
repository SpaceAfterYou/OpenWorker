using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Modules.System.Responses;

/// <summary>
/// Reports a failed request: the opcode of the expected response with the error flag, followed by the code.
/// </summary>
public readonly struct ErrorMessageResponse(MessageOpcode source, ushort code) : IResponseHotspotMessage
{
    private const byte ErrorFlag = 0x80;

    public ErrorMessageResponse(MessageOpcode source, PersonErrorCode code) : this(source, (ushort)code)
    {
    }

    public MessageOpcode Opcode => new(source.Group, (byte)(source.Command | ErrorFlag));

    public void Write(BinaryWriter writer)
    {
        writer.Write(code);
    }
}
