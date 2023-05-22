using Gate.Infrastructure.Gameplay.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Domain.DatabaseModel.Resources;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Cache.Models;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.World;

namespace Gate.Infrastructure.Gameplay.Services;

internal sealed record PersonService : IPersonService
{
    private readonly IHotSpotSession _session;
    private readonly IDbContextFactory<PersistentContext> _factory;
    private readonly IRedisCollection<DistrictModel> _district;

    private readonly ILogger<PersonService> _logger;

    public PersonService(IHotSpotSession session, IDbContextFactory<PersistentContext> factory, IRedisConnectionProvider provider, ILogger<PersonService> logger)
    {
        _session = session;
        _factory = factory;
        _district = provider.RedisCollection<DistrictModel>();

        _logger = logger;
    }

    public async ValueTask Delete(int id, CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Account.FirstOrDefaultAsync(e => e.Id == account.Id, ct) is not AccountModel accountModel)
        {
            _logger.LogWarning("Bad account for delete person. ACCOUNT ID={}", account.Id);
            return;
        }

        if (accountModel.Last?.Id == id)
        {
            accountModel.Last = null;
        }

        if (accountModel.Representative?.Id == id)
        {
            accountModel.Representative = null;
        }

        db.Account.Update(accountModel);

        if (await db.Person.FirstOrDefaultAsync(e => e.Id == id && e.Account.Id == account.Id, ct) is not PersonModel person)
        {
            _logger.LogWarning("Bad person for delete. ID={}, ACCOUNT ID={}", id, account.Id);
            return;
        }

        db.Person.Remove(person);

        await db.SaveChangesAsync(ct);
        await _session.SendAsync(new CharacterListClientMessage(), ct);
    }

    public async ValueTask Join(int id, CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Person.FirstOrDefaultAsync(e => e.Id == id && e.Account.Id == account.Id, ct) is not PersonModel person)
        {
            await _session.SendAsync(new CharacterEnterMapClientMessage { Result = 51001 }, ct);
            return;
        }

        if (await _district.FirstOrDefaultAsync(e => e.Person == id) is not DistrictModel district)
        {
            if (await db.District.FirstOrDefaultAsync(e => e.Id == 50011, ct) is not DistrictResource resource)
            {
                await _session.SendAsync(new CharacterEnterMapClientMessage { Result = 51001 }, ct);
                return;
            }

            district = new DistrictModel
            {
                Id = resource.Id,
            };
        }

        await _session.SendAsync(new CharacterEnterMapClientMessage(), ct);
        await _session.SendAsync(new WorldCurrentDateClientMessage(), ct);
    }
}
