using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Hotspot.Messages.Response.Person;

public readonly struct ZoneValue
{
    public ActorValue Actor { get; init; }
    public int Account { get; init; }
    public int Server { get; init; }
    public int Jump { get; init; }
    public MapValue Map { get; init; }
    public MapValue Parent { get; init; }
    public string Address { get; init; }
    public short Port { get; init; }
    public WorldValue World { get; init; }
    public EnterMapType Type { get; init; }
}