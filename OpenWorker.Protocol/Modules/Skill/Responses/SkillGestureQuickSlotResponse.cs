using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// This packet defined but isn't processed on the client side. Nothing sends it, and writing it throws, so that it is not
/// sent by mistake as an empty body.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct SkillGestureQuickSlotResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.GestureQuickSlot;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        throw new NotImplementedException();
    }

#endregion Interface: IWireWritable
}
