using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Progress of a maze.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct MazeStateValue
{
    public int Maze { get; init; }
    public int Group { get; init; }
    public int District { get; init; }
    public int ClearCount { get; init; }
    public int SharePoint { get; init; }
    public byte RewardStep { get; init; }
}
