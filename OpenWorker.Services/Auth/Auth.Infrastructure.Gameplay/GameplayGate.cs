using DefaultEcs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Communication.Relay.Messages.Requests;
using OpenWorker.Infrastructure.Communication.Relay.Messages.Responses;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.DataTypes;
using SoulWorkerResearch.SoulCore.Defines;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;
using System.Runtime.CompilerServices;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay;

internal sealed record GameplayGate : IGameplayGate
{
    private readonly IDbContextFactory<PersistentContext> _factory;
    private readonly IRequestClient<AvaliableGateRequest> _availableGates;
    private readonly IRequestClient<ConnectGateRequest> _connectGate;

    public GameplayGate(IDbContextFactory<PersistentContext> factory, IRequestClient<AvaliableGateRequest> client, IRequestClient<ConnectGateRequest> connectGate)
    {
        _factory = factory;
        _availableGates = client;
        _connectGate = connectGate;
    }

    public async ValueTask<bool> Join(IHotSpotSession session, ushort id, CancellationToken ct = default)
    {
        var response = await _connectGate.GetResponse<ConnectGateResponse>(new ConnectGateRequest { Id = id }, ct);
        if (response.Message.Result is false) return false;

        await session.SendAsync(new LoginEnterServerClientMessage { Endpoint = response.Message.EndPoint }, ct);

        return true;
    }

    public async ValueTask ShowAvailableGates(Entity entity, IHotSpotSession session, CancellationToken ct = default)
    {
        var gates = GetAvailableGates(entity, ct);

        await session.SendAsync(new LoginUserPersonsForServerResponseClientMessage { Values = await gates.ToArrayAsync(ct), }, ct);
    }

    public async ValueTask UpdateClientFeatures(Entity entity, IHotSpotSession session, CancellationToken ct = default)
    {
        var account = entity.Get<AccountComponent>();

        await session.SendAsync(new LoginContentsInfoClientMessage
        {
            Content = OptionList.Empty,
            AccountId = account.Id,
        }, ct);
    }

    public async IAsyncEnumerable<LoginUserPersonsForServerResponseClientMessage.Entry> GetAvailableGates(Entity entity, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var account = entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);
        var persons = db.Person.Where(e => e.Account.Id == account.Id).ToArray();

        var response = await _availableGates.GetResponse<IEnumerable<AvaliableGateResponse>>(new(), ct);
        foreach (var gate in response.Message.Where(e => e.Status != GateStatus.Offline))
        {
            var count = persons.Where(e => e.Gate.Id == gate.Id).Count();

            yield return new LoginUserPersonsForServerResponseClientMessage.Entry
            {
                Id = gate.Id,
                Name = gate.Name,
                Status = gate.Status,
                Online = gate.Online,
                Person = (byte)Math.Min(count, byte.MaxValue),
            };
        }

        yield break;
    }
}
