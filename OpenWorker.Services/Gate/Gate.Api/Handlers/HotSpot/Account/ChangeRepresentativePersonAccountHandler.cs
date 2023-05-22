using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Account;

public sealed class ChangeRepresentativePersonAccountHandler : IHotSpotHandler<CharacterRepresentativeChangeServerMessage>
{
    private readonly IAccountService _service;

    public ChangeRepresentativePersonAccountHandler(IAccountService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, CharacterRepresentativeChangeServerMessage message, CancellationToken ct) => _service
        .ChangeRepresentativePerson(message.Person.Id, ct);
}
