using DefaultEcs;
using Gate.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace Gate.Api.Handlers.HotSpot.Account;

public sealed class ChangeRepresentativePersonAccountHandler : IHotSpotHandler<CharacterRepresentativeChangeServerMessage>
{
    private readonly IAccountService _service;

    public ChangeRepresentativePersonAccountHandler(IAccountService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, CharacterRepresentativeChangeServerMessage message, CancellationToken ct) => _service
        .ChangeRepresentativePerson(message.Person.Id, ct);
}
