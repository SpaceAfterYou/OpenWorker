using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

/// <summary>
/// What somebody who waits for a party looks like, for the window that shows another player.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyAwaiterInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public required OtherPersonValue Person { get; init; }

    /// <summary>
    /// The titles the window shows next to the name.
    /// </summary>
    public TitleValue Title { get; init; }

    public void Write(BinaryWriter writer)
    {
        Person.Write(writer);
        writer.Write(Title.Value);
    }
}
