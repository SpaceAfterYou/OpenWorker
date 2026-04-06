using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonLoadTitleResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.LoadTitle;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<int> TitleList { get; init; }
    public required IReadOnlyCollection<int> OpenList { get; init; }
    public bool Result { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);

        Write(writer, TitleList);
        Write(writer, OpenList);
    }

    private static void Write(BinaryWriter writer, IReadOnlyCollection<int> list)
    {
        writer.Write((byte)list.Count);

        foreach (var item in list)
        {
            writer.Write(item);
        }
    }

#endregion Interface: IWritableData
}
