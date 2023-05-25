using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Gesture;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Gesture;

public sealed record ShowGestureHandler : IHotSpotHandler<GestureShowServerMessage>
{
    private readonly IGestureService _service;

    public ShowGestureHandler(IGestureService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, GestureShowServerMessage message, CancellationToken ct) => _service
        .Show(message.Gesture, message.Location.Position, message.Location.Rotation, ct);
}
