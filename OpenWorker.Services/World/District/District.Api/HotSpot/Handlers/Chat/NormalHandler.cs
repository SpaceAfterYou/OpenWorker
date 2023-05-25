using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Chat;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Chat;

public sealed record NormalHandler : IHotSpotHandler<ChatNormalServerMessage>
{
    private readonly IChatService _service;

    public NormalHandler(IChatService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, ChatNormalServerMessage message, CancellationToken ct) => _service
        .BroadcastNormal(message.ChatType, message.Message, ct);
}
