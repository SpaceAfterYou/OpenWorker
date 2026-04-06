using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Extensions;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct CharacterTradePasswordResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.TradePassword;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public E_PASSWORD_STATE State { get; init; }
    public int ErrorCode { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(ErrorCode);
        writer.Write(State);
    }

#endregion Interface: IWritableData
}
