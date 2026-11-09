namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// Progress of a maze.
/// </summary>
public readonly struct MazeStateValue
{
    public int Maze { get; init; }
    public int Group { get; init; }
    public int District { get; init; }
    public int ClearCount { get; init; }
    public int SharePoint { get; init; }
    public byte RewardStep { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Maze);
        writer.Write(Group);
        writer.Write(District);
        writer.Write(ClearCount);
        writer.Write(SharePoint);
        writer.Write(RewardStep);
    }
}
