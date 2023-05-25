using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Move;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Gesture;

public sealed record JumpHandler : IHotSpotHandler<MoveJumpBidirectionalMessage>
{
    private readonly IMovementService _service;

    public JumpHandler(IMovementService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, MoveJumpBidirectionalMessage message, CancellationToken ct) => _service
        .Jump(message, ct);
}
