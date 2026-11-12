using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>
/// The skills, the deck and the points of a character, sent once it has entered. Read back too, by the bot of the load test.
/// </summary>
[WireMessage(WireMethods.Both)]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct SkillLoadInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillLoadInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public SkillSnapshotValue Snapshot { get; init; }

#endregion Message: Body
}
