using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;
using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed class SlotChangePersonHandler : IHotSpotHandler<CharacterChangeSlotServerMessage>
{
    private readonly IPersonService _service;

    public SlotChangePersonHandler(IPersonService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, CharacterChangeSlotServerMessage message, CancellationToken ct)
    {
        await _service.SwapSlot(message.Src.SlotId, message.Dst.SlotId, ct);
        await _service.ShowList(ct);
    }
}
