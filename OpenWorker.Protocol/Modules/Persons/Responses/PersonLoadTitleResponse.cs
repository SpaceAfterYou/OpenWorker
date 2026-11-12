using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonLoadTitleResponse : IResponseHotspotMessage
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

    public bool Result { get; init; }

    [WireArray(WireLength.Byte)]
    public required IReadOnlyCollection<int> TitleList { get; init; }

    [WireArray(WireLength.Byte)]
    public required IReadOnlyCollection<int> OpenList { get; init; }

#endregion Message: Body
}
