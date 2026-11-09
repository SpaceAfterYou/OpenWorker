using Microsoft.Extensions.Options;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Handler;

/// <summary>
/// The groups of opcodes that have a bucket of their own. <see cref="PacketRateLimiter.Tag"/> is the value of the
/// <c>group</c> tag of the <c>handler.refused</c> metric.
/// </summary>
public enum PacketGroup
{
    Movement,
    Chat,
    Items,
    Other
}

/// <summary>
/// One token bucket: what is left in it and when it was last filled.
/// </summary>
public struct PacketBucket
{
    public bool Started { get; internal set; }

    public float Tokens { get; internal set; }

    /// <summary>
    /// <see cref="TimeProvider.GetTimestamp"/> of the last fill.
    /// </summary>
    public long At { get; internal set; }
}

/// <summary>
/// The buckets of one session, for <see cref="PacketRateLimiter"/>. The default value is a session that has sent nothing.
/// Touched on the world loop only.
/// </summary>
[EntityComponent(EntityComponentService.All)]
public struct PacketRateComponent
{
    internal PacketBucket Movement;
    internal PacketBucket Chat;
    internal PacketBucket Items;
    internal PacketBucket Other;
}

/// <summary>
/// A token bucket per group of opcodes and per session (<see cref="PacketRateLimitOptions"/>): a packet takes a token of
/// its group, the bucket fills with time up to its burst, and a packet that finds it empty is refused. A refused packet is
/// dropped and counted, not an error: a client that sends too fast loses packets, it does not lose its connection.
/// </summary>
public sealed class PacketRateLimiter
{
    private readonly TimeProvider _time;
    private readonly PacketRateLimitOptions _options;

    // Seconds per tick of the timestamps of the clock, so a fill is one subtraction and one product.
    private readonly double _secondsPerTick;

    public PacketRateLimiter(TimeProvider time, IOptions<PacketRateLimitOptions> options)
    {
        _time = time;
        _options = options.Value;
        _secondsPerTick = 1d / time.TimestampFrequency;
    }

    public static PacketGroup GroupOf(byte group)
    {
        return (GroupOpcode)group switch
        {
            GroupOpcode.Move => PacketGroup.Movement,
            GroupOpcode.Chat => PacketGroup.Chat,
            GroupOpcode.Item or GroupOpcode.Shop or GroupOpcode.Trade or GroupOpcode.ItemUpgrade or GroupOpcode.Drop or GroupOpcode.ItemSetup => PacketGroup.Items,
            _ => PacketGroup.Other
        };
    }

    public static string Tag(PacketGroup group)
    {
        return group switch
        {
            PacketGroup.Movement => "movement",
            PacketGroup.Chat => "chat",
            PacketGroup.Items => "items",
            _ => "other"
        };
    }

    /// <summary>
    /// Takes a token of the group, or says that there is none.
    /// </summary>
    public bool TryTake(ref PacketRateComponent state, PacketGroup group)
    {
        var now = _time.GetTimestamp();

        return group switch
        {
            PacketGroup.Movement => TryTake(ref state.Movement, _options.Movement, now),
            PacketGroup.Chat => TryTake(ref state.Chat, _options.Chat, now),
            PacketGroup.Items => TryTake(ref state.Items, _options.Items, now),
            _ => TryTake(ref state.Other, _options.Other, now)
        };
    }

    private bool TryTake(ref PacketBucket bucket, PacketRateLimit limit, long now)
    {
        bucket.Tokens = bucket.Started
            ? MathF.Min(limit.Burst, bucket.Tokens + (float)((now - bucket.At) * _secondsPerTick * limit.PerSecond))
            : limit.Burst;

        bucket.Started = true;
        bucket.At = now;

        if (bucket.Tokens < 1f)
        {
            return false;
        }

        bucket.Tokens -= 1f;

        return true;
    }
}
