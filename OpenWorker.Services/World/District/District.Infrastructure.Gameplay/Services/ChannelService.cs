using MassTransit;
using Microsoft.Extensions.Configuration;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Cache;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Channel;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed class InstanceChannel
{
    public ushort Id { get; init; }
    public int Online { get; set; }
}

internal sealed class ActiveInstance
{
    internal ushort District { get; }
    internal IReadOnlyCollection<InstanceChannel> Channels { get; }

    public ActiveInstance(IConfiguration configuration)
    {
        District = configuration.GetValue<ushort>("District:Id");

        var min = configuration.GetValue<ushort>("District:MinChannel");
        var max = configuration.GetValue<ushort>("District:MaxChannel");

        Channels = Enumerable.Range(min, max - min).Select(e => new InstanceChannel { Id = (ushort)e }).ToArray();
    }

    internal bool InRange(ushort value) => Channels.Any(e => e.Id == value);
}

internal sealed record GestureService
{
    private readonly IHotSpotSession _session;

    public GestureService(IHotSpotSession session) => _session = session;
}

internal sealed record MovementService
{
    private readonly IHotSpotSession _session;

    public MovementService(IHotSpotSession session) => _session = session;

    internal async ValueTask Jump() { }
    internal async ValueTask LoopMotionEnd() { }
    internal async ValueTask Move() { }
    internal async ValueTask StopBroadcast() { }
}

internal sealed record AuthService
{
    private readonly IHotSpotSession _session;

    public AuthService(IHotSpotSession session) => _session = session;

    internal async ValueTask Join()
    {

    }
}

internal sealed record ChannelService
{
    private readonly IHotSpotSession _session;
    private readonly ActiveInstance _instance;
    private readonly IRedisCollection<ChannelModel> _channels;

    public ChannelService(IHotSpotSession session, ActiveInstance channels, IRedisConnectionProvider provider)
    {
        _session = session;
        _instance = channels;
        _channels = provider.RedisCollection<ChannelModel>();
    }

    internal async ValueTask ShowList(CancellationToken ct = default)
    {
        var entries = _channels.Where(e => e.District == _instance.District).SelectMany(e => e.Entries);

        await _session.SendAsync(new ChannelInfoClientMessage
        {
            Map = _instance.District,
            Values = entries.Select(e => new ChannelInfoClientMessage.Entry { Id = e.Id, Status = e.Status, })
        }, ct);
    }

    internal async ValueTask Switch(ushort id)
    {
        if (_instance.InRange(id))
        {
            _session.Entity.Set<InstanceChannel>();

            // await _session.SendAsync(new ChannelChangeClientMessage() { Channel = id });
            return;
        }
    }
}
