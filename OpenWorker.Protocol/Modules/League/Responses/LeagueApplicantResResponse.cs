using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to your application. The time to wait goes only with the result that says to wait, so the writer is written
/// by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueApplicantResResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.ApplicantRes;

    public required LeagueApplicant Applicant { get; init; }
    public long Remain { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(ref PacketWriter writer)
    {
        Applicant.Write(ref writer);

        // Only an answer that says to wait has the time to wait.
        if (Applicant.Result == 57025)
        {
            writer.WriteInt64(Remain);
        }
    }
}
