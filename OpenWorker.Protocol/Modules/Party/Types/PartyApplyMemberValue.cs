using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct PartyApplyMemberValue
{
    public PartyMemberInfoValue Member { get; init; }
    public int RegDate { get; init; }
}
