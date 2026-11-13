using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The answer to a note: the number of the text in its result, and the minutes to wait for 57022. The time to wait goes
/// only with that result, so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueBoardResResponse : IResponseHotspotMessage, IWireWritable
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.BoardRes;

    public required LeagueBoardEntry Entry { get; init; }
    public long Remain { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(ref PacketWriter writer)
    {
        Entry.Write(ref writer);

        // Only an answer that says to wait has the time to wait.
        if (Entry.Result == 57022)
        {
            writer.WriteInt64(Remain);
        }
    }
}
