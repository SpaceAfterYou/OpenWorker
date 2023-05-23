using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed class ListHandler : IHotSpotHandler<CharacterListServerMessage>
{
    private readonly IPersonService _service;

    public ListHandler(IPersonService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, CharacterListServerMessage request, CancellationToken ct) => _service.
        ShowListAsync(ct);
}
