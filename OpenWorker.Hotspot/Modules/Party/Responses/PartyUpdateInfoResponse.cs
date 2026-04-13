using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Extensions;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyUpdateInfoResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateUserInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public int Party { get; init; } = reader.ReadInt32();
    public ActorValue Master { get; init; } = new(reader);
    public MapValue Maze { get; init; } = new(reader);
    public PartyUpdateType UpdateType { get; init; } = reader.ReadPartyUpdateType();
    public PartyType Type { get; init; } = reader.ReadPartyType();
    public PartyMemberInfoValue[] InfoList { get; init; } = reader.ReadPartyMemberInfoList_Byte();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Party);
        writer.Write(Master);
        writer.Write(Maze);
        writer.Write(UpdateType);
        writer.Write(Type);
        writer.Write_Byte(InfoList);
    }
}
