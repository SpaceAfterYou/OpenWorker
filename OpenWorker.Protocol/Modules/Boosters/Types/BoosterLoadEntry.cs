using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Boosters.Types;

/// <summary>
/// A booster the person has and the seconds it has left; -1 means it does not run out.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct BoosterLoadEntry(short Id, long RemainSeconds) : IWritableData
{
    public const long Endless = -1;
}
