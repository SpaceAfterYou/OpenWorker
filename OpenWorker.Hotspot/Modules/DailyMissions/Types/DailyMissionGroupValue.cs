using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.DailyMissions.Types;

/// <summary>
/// The missions of one district.
/// </summary>
public readonly struct DailyMissionGroupValue : IWritableData
{
    public int District { get; init; }

    public required IReadOnlyCollection<DailyMissionValue> Missions { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(District);
        writer.Write(checked((short)Missions.Count));

        foreach (var mission in Missions)
        {
            mission.Write(writer);
        }
    }
}
