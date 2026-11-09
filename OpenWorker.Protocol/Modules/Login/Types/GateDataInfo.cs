namespace OpenWorker.Hotspot.Modules.Login.Types;

public readonly struct GateDataInfo
{
    public required GateInfo Gate { get; init; }
    public required GatePersonInfo Person { get; init; }
}