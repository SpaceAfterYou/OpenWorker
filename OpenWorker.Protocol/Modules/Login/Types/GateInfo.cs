using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Login.Types;

/// <summary>
/// A gate in the list of the login screen, its fields in the order the client reads them.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct GateInfo
{
    public required short Id { get; init; }
    public required short Port { get; init; }

    [WireString(WireStringFormat.Utf8Ascii)]
    public required string Name { get; init; }

    [WireString(WireStringFormat.Utf8AsciiWithoutTerminator)]
    public required string Address { get; init; }

    public required GateWorkload Workload { get; init; }
    public required int OnlineCount { get; init; }
}
