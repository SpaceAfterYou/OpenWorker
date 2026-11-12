using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person;

/// <summary>
/// The zone a client goes to: the actor and the account it enters with, the maps, the address of the server and the place.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct ZoneValue
{
    public ActorValue Actor { get; init; }
    public int Account { get; init; }
    public int Server { get; init; }
    public int Jump { get; init; }
    public MapValue Map { get; init; }
    public MapValue Parent { get; init; }

    [WireString(WireStringFormat.Utf8AsciiWithoutTerminator)]
    public string Address { get; init; }

    public short Port { get; init; }
    public WorldValue World { get; init; }
    public EnterMapType Type { get; init; }
}
