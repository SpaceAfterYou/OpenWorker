using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The call for new members.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueRecruitNoticeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.RecruitNotice;

    public int League { get; init; }
    public string Notice { get; init; } = string.Empty;
    public long Date { get; init; }
    public int Result { get; init; }
    public long Remain { get; init; }

    public LeagueRecruitNoticeResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(League);
        writer.WriteUtf16UnicodeString(Notice, 51);
        writer.Write(Date);
        writer.Write(Result);
        // Only an answer that says to wait has the time to wait.
        if (Result == 57029)
        {
            writer.Write(Remain);
        }
    }
}
