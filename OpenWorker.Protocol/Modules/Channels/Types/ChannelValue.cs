using OpenWorker.Hotspot.Modules.Channels.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Channels.Types;

/// <summary>
/// A channel in the list of the channels of a map, and how full it is.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct ChannelValue(short Id, [property: WireAs(typeof(byte))] ChannelWorkload Workload);
