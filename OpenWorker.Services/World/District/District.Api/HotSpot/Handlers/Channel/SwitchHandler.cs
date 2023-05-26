using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Channel;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Chat;

public sealed record SwitchHandler : IHotSpotHandler<ChannelChangeServerMessage>
{
    private readonly IChannelService _service;

    public SwitchHandler(IChannelService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, ChannelChangeServerMessage message, CancellationToken ct) => _service
        .Switch(message.Channel, ct);
}
