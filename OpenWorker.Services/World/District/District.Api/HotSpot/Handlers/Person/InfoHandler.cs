using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Person;

internal class InfoHandler : IHotSpotHandler<CharacterInfoServerMessage>
{
    private readonly IPersonService _person;

    public InfoHandler(IPersonService person) => _person = person;

    public ValueTask OnHandleAsync(Entity entity, CharacterInfoServerMessage request, CancellationToken ct) => _person.SendCurrent(ct);
}
