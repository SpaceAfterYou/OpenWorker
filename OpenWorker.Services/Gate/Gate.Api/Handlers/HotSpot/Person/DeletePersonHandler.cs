using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed class DeletePersonHandler : IHotSpotHandler<CharacterDeleteServerMessage>
{
    private readonly IPersonService _service;

    public DeletePersonHandler(IPersonService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, CharacterDeleteServerMessage message, CancellationToken ct)
    {
        await _service.Delete(message.Character, ct);
        await _service.ShowList(ct);
    }
}
