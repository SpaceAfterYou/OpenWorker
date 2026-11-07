using StackExchange.Redis;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Redis publish and subscribe, the same Redis the servers already share.
/// </summary>
public sealed class RedisCommunityTransport(IConnectionMultiplexer redis) : ICommunityTransport
{
    public async ValueTask PublishAsync(string channel, ReadOnlyMemory<byte> payload)
    {
        await redis
            .GetSubscriber()
            .PublishAsync(RedisChannel.Literal(channel), payload);
    }

    public async ValueTask SubscribeAsync(string channel, Action<ReadOnlyMemory<byte>> handler)
    {
        await redis
            .GetSubscriber()
            .SubscribeAsync(RedisChannel.Literal(channel), (_, value) =>
            {
                if (value.HasValue)
                {
                    handler((byte[])value!);
                }
            });
    }
}
