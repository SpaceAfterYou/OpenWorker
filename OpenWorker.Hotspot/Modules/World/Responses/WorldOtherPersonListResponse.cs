using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldOtherPersonListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.OtherInfosPc;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<PersonWorldPair> People { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)People.Count);

        foreach (var pair in People)
        {
            writer.Write(pair.Person);
            writer.Write(pair.World);
        }
    }
}
