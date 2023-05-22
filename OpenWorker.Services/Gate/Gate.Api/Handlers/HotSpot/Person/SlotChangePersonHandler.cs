using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;
using DefaultEcs;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using Gate.Infrastructure.Gameplay.Abstractions;

namespace OpenWorker.Services.Login.Application.Net.Person;

public sealed class SlotChangePersonHandler : IHotSpotHandler<CharacterChangeSlotServerMessage>
{
    private readonly IPersonService _service;

    public SlotChangePersonHandler(IPersonService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, CharacterChangeSlotServerMessage message, CancellationToken ct)
    {
        var persons = entity.Get<PersonList>();
        persons.TrySwap(message.Src.CharacterId, message.Src.SlotId, message.Dst.CharacterId, message.Dst.SlotId);

        await entity.Get<ISyncSession>().SendAsync(new CharacterListClientMessage
        {
            Values = persons.Values.ToArray()
        }, ct);
    }
}
