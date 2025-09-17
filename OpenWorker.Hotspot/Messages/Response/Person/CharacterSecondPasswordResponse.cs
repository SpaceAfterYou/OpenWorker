using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Extensions;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, Command)]
public readonly struct CharacterSecondPasswordResponse(E_PASSWORD_STATE state, int errorCode) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.SecondPassword;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(errorCode);
        writer.Write(state);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct CharacterTradePasswordResponse(E_PASSWORD_STATE state, int errorCode) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.TradePassword;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(errorCode);
        writer.Write(state);
    }
}