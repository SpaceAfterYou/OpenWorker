using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;
using DefaultEcs;
using Microsoft.Extensions.Logging;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;

namespace OpenWorker.Services.Login.Application.Net.Person;

public sealed class PersonSlotChangeHandler : IHotSpotHandler<CharacterChangeSlotServerMessage>
{
    public async ValueTask OnHandleAsync(Entity entity, CharacterChangeSlotServerMessage message, CancellationToken ct)
    {
        var persons = entity.Get<PersonList>();
        persons.TrySwap(message.Src.CharacterId, message.Src.SlotId, message.Dst.CharacterId, message.Dst.SlotId);

        await entity.Get<ISyncSession>().SendAsync(new CharacterListClientMessage
        {
            Values = persons.Values.ToArray()
        }, ct);
    }

    public PersonSlotChangeHandler(ILogger<PersonSlotChangeHandler> logger)
    {
        _logger = logger;
    }

    private readonly ILogger _logger;
}
