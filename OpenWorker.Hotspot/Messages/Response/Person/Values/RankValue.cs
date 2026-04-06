namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct RankValue
{
    public byte Level { get; }
    public int Experience { get; }

    public RankValue(byte level, int experience)
    {
        Level = level;
        Experience = experience;
    }

    public RankValue(BinaryReader reader)
    {
        Level = reader.ReadByte();
        Experience = reader.ReadInt32();
    }
}
