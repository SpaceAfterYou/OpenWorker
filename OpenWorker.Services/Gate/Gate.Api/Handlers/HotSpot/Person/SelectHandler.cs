using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Service.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed record SelectHandler : IHotSpotHandler<CharacterSelectServerMessage>
{
    private readonly IPersonService _personService;
    private readonly IServerService _serverService;

    public SelectHandler(IPersonService service, IServerService serverService)
    {
        _personService = service;
        _serverService = serverService;
    }

    public async ValueTask OnHandleAsync(Entity entity, CharacterSelectServerMessage message, CancellationToken ct)
    {
        await _serverService.SyncDateTime(ct);
        await _personService.SelectAsync(message.Character, ct);
    }
}
