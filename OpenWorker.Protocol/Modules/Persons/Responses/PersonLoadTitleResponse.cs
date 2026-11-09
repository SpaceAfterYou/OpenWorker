using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonLoadTitleResponse : IResponseHotspotMessage
{
    /// <summary>
    /// The client reads the length of a list as a signed byte, and takes nothing from a longer one.
    /// </summary>
    public const int MaxListLength = sbyte.MaxValue;

#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.LoadTitle;

    public MessageOpcode Opcode => new(Group, (byte)Command);

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
        Debug.Assert(list.Count <= MaxListLength);

        writer.Write((byte)list.Count);

        foreach (var item in list)
        {
            writer.Write(item);
        }
    }

#endregion Interface: IWritableData
}
