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
/// The answer to a note: the number of the text in its result, and the minutes to wait for 57022.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueBoardResResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.BoardRes;

    public required LeagueBoardEntry Entry { get; init; }
    public long Remain { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Entry);
        // Only an answer that says to wait has the time to wait.
        if (Entry.Result == 57022)
        {
            writer.Write(Remain);
        }
    }
}
