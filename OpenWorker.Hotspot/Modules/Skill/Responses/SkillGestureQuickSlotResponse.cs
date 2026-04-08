using System.Linq;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// Skill-group opcode <c>0x78</c>: client dispatcher jumps to <c>unknown_libname_8</c> (empty stub in this idb).
/// Six int32 values mirror the gesture quick-slot count used in the gesture module — treat as skill ids per emote slot until the real layout is recovered.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillGestureQuickSlotResponse(BinaryReader reader) : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.GestureQuickSlot;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>One entry per gesture/emote quick slot (length = <see cref="GesturesModuleDefines.MaxGestureCount"/>).</summary>
    public int[] GestureSkillIds { get; init; } = Enumerable
        .Range(0, GesturesModuleDefines.MaxGestureCount)
        .Select(_ => reader.ReadInt32())
        .ToArray();

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        foreach (var id in GestureSkillIds)
        {
            writer.Write(id);
        }
    }

#endregion Interface: IWritableData
}
