using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Skill.Responses;

/// <summary>Ответ на активный скилл: код ошибки, divergence, swap.</summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SkillActiveSkillResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ActiveSkillRes;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>0 — без ошибки. Код сообщения в tb_(.*?)_script.res</summary>
    public int ErrorCode { get; init; }

    /// <summary>tb_divergence.res</summary>
    public int Divergence { get; init; }

    public bool Swap { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(ErrorCode);
        writer.Write(Divergence);
        writer.Write(Swap);
    }

#endregion Interface: IWritableData
}
