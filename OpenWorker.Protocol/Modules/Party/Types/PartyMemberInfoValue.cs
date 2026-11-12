using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct PartyMemberInfoValue
{
    /// <summary>
    /// A free place in a list of fixed size.
    /// </summary>
    public static PartyMemberInfoValue Empty { get; } = new() { Name = string.Empty };

    public ActorValue Actor { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
    public byte Level { get; init; }
    [WireAs(typeof(byte))]
    public Hero Class { get; init; }
    public int Location { get; init; }
    public int Channel { get; init; }
    public int MaxHealth { get; init; }
    public int Health { get; init; }
    public bool IsLoggedIn { get; init; }
    public MapValue Map { get; init; }
}
