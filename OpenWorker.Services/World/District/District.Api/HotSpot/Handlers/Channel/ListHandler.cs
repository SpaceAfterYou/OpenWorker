using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Channel;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Chat;

public sealed record ListHandler : IHotSpotHandler<ChannelInfoServerMessage>
{
    private readonly IChannelService _service;

    public ListHandler(IChannelService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, ChannelInfoServerMessage message, CancellationToken ct) => _service
        .ShowList(ct);
}
