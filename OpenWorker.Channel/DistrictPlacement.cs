using OpenWorker.Hotspot.Cache.Types;

namespace OpenWorker.Channel;

/// <summary>
/// The district server and the channel a player is sent to.
/// </summary>
public sealed record DistrictPlacement(DistrictCache District, ChannelCache Channel);
