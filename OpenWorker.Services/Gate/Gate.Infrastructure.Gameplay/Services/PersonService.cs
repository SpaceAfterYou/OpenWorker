using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.World;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Services;

internal sealed record PersonService : IPersonService
{
    private readonly IHotSpotSession _session;
    private readonly IDbContextFactory<PersistentContext> _factory;

    private readonly ILogger<PersonService> _logger;

    public PersonService(IHotSpotSession session, IDbContextFactory<PersistentContext> factory, ILogger<PersonService> logger)
    {
        _session = session;
        _factory = factory;

        _logger = logger;
    }

    public async ValueTask ShowList(CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);
        var persons = db.Person.AsNoTracking().Where(e => e.Account.Id == account.Id);

        await _session.SendAsync(new CharacterListClientMessage(), ct);
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
    }

    public async ValueTask Join(int id, CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Person.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.Account.Id == account.Id, ct) is not PersonModel person)
        {
            await _session.SendAsync(new CharacterEnterMapClientMessage { Result = 51001 }, ct);
            return;
        }

        await _session.SendAsync(new CharacterEnterMapClientMessage(), ct);
        await _session.SendAsync(new WorldCurrentDateClientMessage(), ct);
    }

    public async ValueTask SwapSlot(byte left, byte right, CancellationToken ct = default)
    {
        var account = _session.Entity.Get<AccountComponent>();

        await using var db = await _factory.CreateDbContextAsync(ct);

        await ChangeSlot(db, left, right, account.Id, ct);
        await ChangeSlot(db, right, left, account.Id, ct);

        await _session.SendAsync(new CharacterListClientMessage(), ct);
    }

    private static async ValueTask ChangeSlot(PersistentContext db, byte old, byte @new, int account, CancellationToken ct)
    {
        if (await db.Person.FirstOrDefaultAsync(e => e.Id == old && e.Account.Id == account, ct) is PersonModel model)
        {
            model.Slot = @new;
            db.Person.Update(model);
        }
    }
}
