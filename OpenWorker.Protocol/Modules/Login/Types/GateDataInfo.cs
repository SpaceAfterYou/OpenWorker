using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct GateDataInfo
{
    public required GateInfo Gate { get; init; }
    public required GatePersonInfo Person { get; init; }
}
