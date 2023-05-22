using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Person;

public sealed class JoinPersonHandler : IHotSpotHandler<CharacterSelectServerMessage>
{
    private readonly IWorldService _service;

    public JoinPersonHandler(IWorldService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, CharacterSelectServerMessage message, CancellationToken ct)
    {
        await _service.Join(ct);

        //var persons = entity.Get<PersonList>();

        //if (!persons.TryGetValue(message.CharacterId, out var person))
        //    return;

        //if (!session.Server.Districts.TryGetValue(person.Place.District, out var district))
        //    return;

        //if (!district.Relay.Session.Reserve(new RWCSessionReserveRequest() { Character = person.Id }).Result)
        //{
        //    await session.SendAsync(new EnterMapResponse { Result = 50011 });
        //}

        //persons.Last = person;

        //var session = entity.Get<ISyncSession>();

        //await session.SendAsync(new WorldCurrentDateClientMessage());
        //await session.SendAsync(new EnterMapResponse
        //{
        //    AccountId = session.AccountId,
        //    CharacterId = person.Id,
        //    Pos = new PositionInfoPacketSharedStructure()
        //    {
        //        LocationId = person.Place.District,
        //        Position = person.Place.Postion,
        //        Rotation = person.Place.Rotation
        //    },
        //    EndPoint = new SEndPointSharedResponse()
        //    {
        //        Ip = district.Ip,
        //        Port = district.Port
        //    }
        //});
    }
}
