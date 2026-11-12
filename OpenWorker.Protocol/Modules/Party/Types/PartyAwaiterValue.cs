using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Types;

/// <summary>
/// Someone waiting to be picked for a party.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct PartyAwaiterValue
{
    public ActorValue Actor { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
    public short Level { get; init; }
    [WireAs(typeof(byte))]
    public Hero Class { get; init; }
    public int RemainTime { get; init; }

    // The client keeps these two but nothing found so far uses them.
    public int Unknown1 { get; init; }
    public int Unknown2 { get; init; }
}
