using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Events.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct EventLuaValuesResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.LuaValues;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<string> Scripts { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Scripts.Count);

        foreach (var script in Scripts)
        {
            writer.WriteUtf8AsciiStringWithoutTerminator(script);
        }
    }

#endregion Interface: IWritableData
}
