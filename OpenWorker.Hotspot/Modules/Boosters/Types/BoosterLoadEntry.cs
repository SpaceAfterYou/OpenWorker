using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Boosters.Types;

/// <summary>
/// A booster the person has and the seconds it has left; -1 means it does not run out.
/// </summary>
public readonly record struct BoosterLoadEntry(short Id, long RemainSeconds) : IWritableData
{
    public const long Endless = -1;

    public void Write(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(RemainSeconds);
    }
}
