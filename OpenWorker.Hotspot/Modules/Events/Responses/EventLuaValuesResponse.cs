using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Events.Responses;

[HotspotMessage(Group, Command)]
public readonly record struct EventLuaValuesResponse(IReadOnlyCollection<string> Scripts) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.LuaValues;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write((byte)Scripts.Count);
        
        foreach (var script in Scripts)
        {
            writer.WriteUtf8AsciiStringWithoutTerminator(script);
        }
    }
}