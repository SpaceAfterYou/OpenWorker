using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryCompleteListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.CompleteList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// A bit for every finished soul metry, <see cref="SoulMetryModuleDefines.CompleteListByteLength"/> bytes.
    /// </summary>
    public required byte[] Complete { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        if (Complete.Length != SoulMetryModuleDefines.CompleteListByteLength)
        {
            throw new InvalidOperationException("The list of finished soul metries has a fixed length.");
        }

        writer.Write(Complete);
    }

#endregion Interface: IWritableData
}
