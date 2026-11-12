using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct SkillProjectileRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ProjectileReq;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Skill { get; }
    public short TriggerIndex { get; }
    public Vector3 Position { get; }
    public Vector3 Direction { get; }
    public Vector3 Right { get; }
    public float Yaw { get; }
    public float Pitch { get; }
    public short AdditionalDirectionX { get; }
    public short AdditionalDirectionY { get; }
    public uint Session { get; }
    public ActorValue Target { get; }

#endregion Message: Body
}
