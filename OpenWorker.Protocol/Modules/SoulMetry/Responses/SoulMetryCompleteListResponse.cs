using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

/// <summary>
/// The soul metries the person has finished. The list has one length, and a list of another is a bug of the caller that
/// is refused rather than cut, so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryCompleteListResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.CompleteList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// A bit for every finished soul metry, <see cref="SoulMetryModuleDefines.CompleteListByteLength"/> bytes.
    /// </summary>
    public required byte[] Complete { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        if (Complete.Length != SoulMetryModuleDefines.CompleteListByteLength)
        {
            throw new InvalidOperationException("The list of finished soul metries has a fixed length.");
        }

        writer.WriteBytes(Complete);
    }

#endregion Interface: IWireWritable
}
