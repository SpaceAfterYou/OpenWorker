using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Move;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

public interface IMovementService
{
    ValueTask Stop(MoveStopBidirectionalMessage message, CancellationToken ct = default);
    ValueTask Jump(MoveJumpBidirectionalMessage message, CancellationToken ct = default);
    ValueTask Move(MoveMoveBidirectionalMessage message, CancellationToken ct = default);
}

internal sealed record MovementService : IMovementService
{
    private readonly IHotSpotSession _session;

    public MovementService(IHotSpotSession session) => _session = session;

    // public async ValueTask LoopMotionEnd(CancellationToken ct = default) { }

    public ValueTask Stop(MoveStopBidirectionalMessage message, CancellationToken ct = default) => _session.Entity
        .Get<InstanceChannel>()
        .BroadcastMovement(message, ct);

    public ValueTask Jump(MoveJumpBidirectionalMessage message, CancellationToken ct = default) => _session.Entity
        .Get<InstanceChannel>()
        .BroadcastMovement(message, ct);

    public ValueTask Move(MoveMoveBidirectionalMessage message, CancellationToken ct = default) => _session.Entity
        .Get<InstanceChannel>()
        .BroadcastMovement(message, ct);
}
