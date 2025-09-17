using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command)]
public readonly struct WorldOutInfoNpcResponse(Arch.Core.World world, params Entity[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.OutInfoNpc;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActorList(world, list);
    }
}