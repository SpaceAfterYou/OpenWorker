﻿using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Gesture;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Gesture;

public sealed record ChangeSlotHandler : IHotSpotHandler<GestureSlotUpdateBidirectionalMessage>
{
    private readonly IGestureService _service;

    public ChangeSlotHandler(IGestureService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, GestureSlotUpdateBidirectionalMessage message, CancellationToken ct) => _service
        .ChangeSlot(message.Values, ct);
}
