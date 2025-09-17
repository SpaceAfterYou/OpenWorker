using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Components;

namespace OpenWorker.Hotspot.Modules.Gestures.Responses;

[HotspotMessage(Group, Command)]
public readonly struct GestureSlotLoadResponse(Arch.Core.World world, Entity player) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Gesture;
    private const GestureOpcode Command = GestureOpcode.SlotLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        foreach (var identifier in world.Get<GestureComponent>(player).Collection)
        {
            writer.Write(identifier);
        }
    }
}