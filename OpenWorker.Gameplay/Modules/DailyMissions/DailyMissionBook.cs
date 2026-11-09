using System.Text.Json.Serialization;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Gameplay.Modules.DailyMissions;

/// <summary>
/// One mission of a person. This is what is stored.
/// </summary>
public sealed class DailyMissionEntry
{
    public int Mission { get; set; }

    public DailyMissionState State { get; set; }

    public bool Helper { get; set; }

    public short Value { get; set; }

    public DateTimeOffset Accept { get; set; }

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }
}

/// <summary>
/// The missions drawn for a person.
/// </summary>
public sealed class DailyMissionBook
{
    public List<DailyMissionEntry> Missions { get; set; } = [];

    /// <summary>
    /// The missions in the tracker.
    /// </summary>
    [JsonIgnore]
    public int HelperCount => Missions.Count(e => e is { State: DailyMissionState.Accept, Helper: true });

    public DailyMissionEntry? Find(int mission) => Missions.Find(e => e.Mission == mission);

    /// <summary>
    /// Drops the missions the tables do not know.
    /// </summary>
    public void Normalize(DailyMissionCatalog catalog)
    {
        Missions.RemoveAll(e => !catalog.TryGet(e.Mission, out _));
        Missions = Missions.DistinctBy(e => e.Mission).ToList();

        // Only a mission being done is in the tracker.
        foreach (var entry in Missions.Where(e => e.State != DailyMissionState.Accept))
        {
            entry.Helper = false;
        }
    }
}
