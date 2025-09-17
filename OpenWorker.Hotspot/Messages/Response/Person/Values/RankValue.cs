using OpenWorker.Hotspot.Messages.Response.Person.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct RankValue
{
    public byte Level { get; }
    public int Experience { get; }

    public RankValue(RankComponent component)
    {
        Level = component.Level;
        Experience = component.Experience;
    }

    public RankValue(BinaryReader reader)
    {
        Level = reader.ReadByte();
        Experience = reader.ReadInt32();
    }
}