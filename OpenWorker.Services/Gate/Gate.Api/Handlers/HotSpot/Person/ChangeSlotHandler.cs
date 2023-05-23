using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;
using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed record ChangeSlotHandler : IHotSpotHandler<CharacterChangeSlotServerMessage>
{
    private readonly IPersonService _service;

    public ChangeSlotHandler(IPersonService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, CharacterChangeSlotServerMessage message, CancellationToken ct)
    {
        await _service.SwapSlotAsync(message.Src.SlotId, message.Dst.SlotId, ct);
        await _service.ShowListAsync(ct);
    }
}
