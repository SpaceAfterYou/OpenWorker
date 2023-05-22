using DefaultEcs;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Cache.Models;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Extensions;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.DataTypes;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;
using System.Runtime.CompilerServices;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Services;

internal sealed record GateService : IGateService
{
    private readonly IHotSpotSession _session;
    private readonly IRedisCollection<GateModel> _gate;
    private readonly IDbContextFactory<PersistentContext> _factory;

    public GateService(IHotSpotSession session, IRedisConnectionProvider client, IDbContextFactory<PersistentContext> factory)
    {
        _session = session;
        _gate = client.RedisCollection<GateModel>();
        _factory = factory;
    }

    public async ValueTask<bool> JoinAsync(ushort id, CancellationToken ct = default)
    {
        if (await _gate.FirstOrDefaultAsync(e => e.Id == id) is not GateModel gateModel)
        {
            return false;
        }

        if (!gateModel.CanJoin)
        {
            return false;
        }

        await _session.SendAsync(new LoginEnterServerClientMessage { Endpoint = gateModel.EndPoint }, ct);

        return true;
    }

    public async ValueTask ShowAvailableGatesAsync(CancellationToken ct = default)
    {
        var gates = GetAvailableGates(ct);

        await _session.SendAsync(new LoginUserPersonsForServerResponseClientMessage { Values = await gates.ToArrayAsync(ct), }, ct);
    }

    public async ValueTask ShowEnabledFeaturesAsync(CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await _session.SendAsync(new LoginContentsInfoClientMessage
        {
            Content = OptionList.Empty,
            AccountId = account.Id,
        }, ct);
    }

    public async IAsyncEnumerable<LoginUserPersonsForServerResponseClientMessage.Entry> GetAvailableGates([EnumeratorCancellation] CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);
        var persons = await db.Person.Where(e => e.Account.Id == account.Id).ToArrayAsync(ct);

        await foreach (var gate in _gate.GetOnlineGates())
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
    }
}
