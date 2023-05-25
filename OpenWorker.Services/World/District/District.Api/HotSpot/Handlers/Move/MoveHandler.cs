using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Move;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Gesture;

public sealed record MoveHandler : IHotSpotHandler<MoveMoveBidirectionalMessage>
{
    private readonly IMovementService _service;

    public MoveHandler(IMovementService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, MoveMoveBidirectionalMessage message, CancellationToken ct) => _service
        .Move(message, ct);
}
