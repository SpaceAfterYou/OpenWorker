using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct GatePersonInfo
{
    public required byte Count { get; init; }
}
