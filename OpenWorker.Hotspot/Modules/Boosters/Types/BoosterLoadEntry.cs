namespace OpenWorker.Hotspot.Modules.Boosters.Types;

public readonly struct BoosterLoadEntry(short id, TimeSpan remaining)
{
    public short Id => id;
    public TimeSpan Remaining => remaining;
}