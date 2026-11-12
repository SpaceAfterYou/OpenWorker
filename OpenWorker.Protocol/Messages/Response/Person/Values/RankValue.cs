using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct RankValue : IWireReadable<RankValue>
{
    public byte Level { get; }
    public int Experience { get; }

    public RankValue(byte level, int experience)
    {
        Level = level;
        Experience = experience;
    }

    public static RankValue Read(ref PacketReader reader)
    {
        var level = reader.ReadByte();
        var experience = reader.ReadInt32();

        return new RankValue(level, experience);
    }
}
