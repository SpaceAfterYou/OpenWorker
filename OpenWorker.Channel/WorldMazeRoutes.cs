using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.Channel;

/// <summary>
/// Which instance of an open maze of the world a player goes into: the one of the channel it stands on, while that has room,
/// else the first other channel of this server with room. The record of each channel in Redis
/// (<see cref="WorldMazeDirectory"/>) sends everybody of the channel into the same instance.
/// </summary>
public sealed partial class WorldMazeRoutes(
    World world,
    WorldMazes worlds,
    WorldMazeDirectory directory,
    IOptions<GateOptions> gate,
    ILogger<WorldMazeRoutes> logger,
    ServiceChannels? channels = null)
{
    /// <summary>
    /// The channel a player of a server without channels (a maze) goes to.
    /// </summary>
    private const int FirstChannel = 1;

    private short Gate { get; } = gate.Value.Id;

    public bool IsWorld(short location) => worlds.IsWorld(location);

    /// <summary>
    /// The instance and its channel for the player; nothing when every channel is full or the records cannot be read.
    /// <paramref name="server"/> is where a new instance is opened.
    /// </summary>
    public async ValueTask<(WorldMazeRoute Route, int Channel)?> RouteAsync(Entity player, short location, (Guid Server, string Host, short Port) server)
    {
        foreach (var channel in ChannelsOf(player))
        {
            WorldMazeRoute? route;

            try
            {
                route = await directory.RouteAsync(Gate, location, channel, server, WorldMazes.Capacity);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogRouteFailed(e, location, channel);
                return null;
            }

            if (route is { } found)
            {
                return (found, channel);
            }

            if (!world.IsAlive(player))
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// The channel of the player first, then the others of this server in their order.
    /// </summary>
    private IEnumerable<int> ChannelsOf(Entity player)
    {
        if (channels is not { Count: > 0 } || !world.IsAlive(player) || !world.Has<ChannelMemberComponent>(player))
        {
            yield return FirstChannel;
            yield break;
        }

        var own = channels.Get(player).Identifier;

        yield return own;

        foreach (var channel in channels)
        {
            if (channel.Identifier != own)
            {
                yield return channel.Identifier;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The record of the open maze {Location} on channel {Channel} could not be read")]
    private partial void LogRouteFailed(Exception exception, short location, int channel);
}
