using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Skill.Requests;

/// <summary>
/// A player starts or ends charging a skill (06/4C). The client sends the one opcode for both: the start has no body, the end
/// has the time the key was held, as the four bytes of a float. The request tells the two apart by the bytes it finds, so its
/// reader is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly struct SkillChargingRequest : IRequestHotspotMessage, IWireReadable<SkillChargingRequest>
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillChargingStart;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

    public SkillChargingRequest(BinaryReader reader)
    {
        this = WireBridge.Read<SkillChargingRequest>(reader);
    }

    /// <summary>
    /// The player let the key go: the charging ends. Otherwise it starts.
    /// </summary>
    public bool Released { get; init; }

    /// <summary>
    /// How long the key was held, of a request that ends the charging; it is whatever the client sent.
    /// </summary>
    public float PressTime { get; init; }

    public static SkillChargingRequest Read(ref PacketReader reader)
    {
        var released = reader.Remaining >= sizeof(float);

        return new SkillChargingRequest { Released = released, PressTime = released ? reader.ReadSingle() : 0f };
    }
}
