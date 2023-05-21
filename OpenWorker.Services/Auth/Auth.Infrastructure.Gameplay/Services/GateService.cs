﻿using DefaultEcs;
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
    private readonly IRedisCollection<GateModel> _gate;
    private readonly IDbContextFactory<PersistentContext> _factory;

    public GateService(IRedisConnectionProvider client, IDbContextFactory<PersistentContext> factory)
    {
        _gate = client.RedisCollection<GateModel>();
        _factory = factory;
    }

    public async ValueTask<bool> JoinAsync(IHotSpotSession session, ushort id, CancellationToken ct = default)
    {
        if (await _gate.FirstOrDefaultAsync(e => e.Id == id) is not GateModel gateModel)
        {
            return false;
        }

        if (!gateModel.CanJoin)
        {
            return false;
        }

        await session.SendAsync(new LoginEnterServerClientMessage { Endpoint = gateModel.EndPoint }, ct);

        return true;
    }

    public async ValueTask ShowAvailableGatesAsync(Entity entity, IHotSpotSession session, CancellationToken ct = default)
    {
        var gates = GetAvailableGates(entity, ct);

        await session.SendAsync(new LoginUserPersonsForServerResponseClientMessage { Values = await gates.ToArrayAsync(ct), }, ct);
    }

    public async ValueTask UpdateClientFeaturesAsync(Entity entity, IHotSpotSession session, CancellationToken ct = default)
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

        yield break;
    }
}
