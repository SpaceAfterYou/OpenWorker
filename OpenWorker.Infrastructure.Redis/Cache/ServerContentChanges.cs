using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Cache;

/// <summary>
/// What a server does when a content is switched: a district or a maze tells its players. Runs on the world loop.
/// </summary>
public interface IServerContentListener
{
    ValueTask OnSwitchedAsync(ServerContentIndex index, bool state, CancellationToken cancellationToken);
}

/// <summary>
/// Switches a content on every server at once. The client must hear the same from every server: the auth server enables
/// the entries of the menu at the login, a district or a maze by 01/07 later. The server that asks takes the switch at once
/// and tells its listeners; then the switch goes to the other servers through Redis, and each does the same on its loop. A server that starts
/// later reads the table, which is written before the switch is sent.
/// </summary>
public sealed partial class ServerContentChanges(
    ServerContent contents,
    IConnectionMultiplexer redis,
    WorldLoop loop,
    IEnumerable<IServerContentListener> listeners,
    ILogger<ServerContentChanges> logger)
{
    private static RedisChannel Channel { get; } = RedisChannel.Literal("ow:server-contents");

    /// <summary>
    /// Names this process in its switches, so it does not take its own back from Redis: it has applied them already, and a
    /// late echo could undo a later switch.
    /// </summary>
    private Guid Origin { get; } = Guid.NewGuid();

    /// <summary>
    /// Starts taking the switches of the other servers. A server listens before it reads the table, so no switch falls
    /// between the two.
    /// </summary>
    public async Task ListenAsync()
    {
        await redis.GetSubscriber().SubscribeAsync(Channel, (_, value) => Receive(value));
    }

    /// <summary>
    /// Called on a thread of Redis: the switch is handed to the world loop.
    /// </summary>
    private void Receive(RedisValue value)
    {
        if (value.HasValue && (byte[])value! is { Length: 18 } body && new Guid(body.AsSpan(2)) != Origin)
        {
            _ = loop.InvokeAsync(() => ApplyAsync(body[0], body[1] != 0));
        }
    }

    /// <summary>
    /// Switches the content here, its players told, then tells every other server. Runs on the world loop: a request that
    /// follows the switch on this server sees it. The caller writes the table first.
    /// </summary>
    public async Task SwitchAsync(ServerContentIndex index, bool state)
    {
        await ApplyAsync((byte)index, state);

        var body = new byte[18];

        body[0] = (byte)index;
        body[1] = (byte)(state ? 1 : 0);
        Origin.TryWriteBytes(body.AsSpan(2));

        await redis.GetSubscriber().PublishAsync(Channel, body);
    }

    private async ValueTask ApplyAsync(byte index, bool state)
    {
        if (index >= contents.Count || contents[index] == state)
        {
            return;
        }

        contents[index] = state;

        LogSwitched(logger, (ServerContentIndex)index, state);

        foreach (var listener in listeners)
        {
            try
            {
                await listener.OnSwitchedAsync((ServerContentIndex)index, state, CancellationToken.None);
            }

            catch (Exception e)
            {
                LogListenerFailed(logger, e, (ServerContentIndex)index);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Content {Index} switched to {State}.")]
    private static partial void LogSwitched(ILogger logger, ServerContentIndex index, bool state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The players could not all be told that content {Index} was switched.")]
    private static partial void LogListenerFailed(ILogger logger, Exception exception, ServerContentIndex index);
}
