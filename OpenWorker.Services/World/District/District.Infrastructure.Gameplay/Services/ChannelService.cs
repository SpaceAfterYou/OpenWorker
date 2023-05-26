using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Cache;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Change;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed record ChannelService : IChannelService
{
    private readonly ThisInstance _instance;
    private readonly IHotSpotSession _session;
    private readonly IRedisCollection<ChannelModel> _channels;

    public ChannelService(ThisInstance instance, IHotSpotSession session, IRedisConnectionProvider provider)
    {
        _instance = instance;
        _session = session;
        _channels = provider.RedisCollection<ChannelModel>();
    }

    public ValueTask Join(ushort id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask Leave(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask ShowList(CancellationToken ct = default)
    {
        var entries = _channels.Where(e => e.District == _instance.District).SelectMany(e => e.Entries);

        await _session.SendAsync(new ChannelInfoClientMessage
        {
            Map = _instance.District,
            Values = entries.Select(e => new ChannelInfoClientMessage.Entry { Id = e.Id, Status = e.Status, })
        }, ct);
    }

    public async ValueTask Switch(ushort id, CancellationToken ct = default)
    {
        if (_instance.Channels.Any(e => e.Id == id))
        {
            _session.Entity.Set<InstanceChannel>();

            // await _session.SendAsync(new ChannelChangeClientMessage() { Channel = id });
            return;
        }
        
        // TODO: connect to another instance
        //       request to relay
    }
}
