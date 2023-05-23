using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Services;

internal sealed record AccountService : IAccountService
{
    private readonly IHotSpotSession _session;
    private readonly IDbContextFactory<PersistentContext> _factory;

    private readonly ILogger _logger;

    public AccountService(IHotSpotSession session, IDbContextFactory<PersistentContext> factory, ILogger<AccountService> logger)
    {
        _session = session;
        _factory = factory;

        _logger = logger;
    }

    public async ValueTask ChangeBackgroundAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (await db.CharacterBackground.AnyAsync(e => e.Id == id, ct) is not true)
        {
            _logger.LogWarning("Bad requested background. ID={}", id);
            return;
        }

        var account = _session.Entity.Get<AccountComponent>();
        await _session.SendAsync(new CharacterBackgroundChangeClientMessage { Account = account.Id, Background = id }, ct);
    }

    public async ValueTask ChangeRepPersonAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var account = _session.Entity.Get<AccountComponent>();
        if (await db.Person.FirstOrDefaultAsync(e => e.Id == id && e.Account.Id == account.Id, ct) is not PersonModel person)
        {
            _logger.LogWarning("Bad requested person. ID={}, ACCOUNT ID={}", id, account.Id);
            return;
        }

        if (person.Id == person.Account.Representative?.Id)
        {
            _logger.LogDebug("Bad requested representative person. Same already set. ID={}", id);
            return;
        }

        await _session.SendAsync(new CharacterRepresentativeChangeClientMessage
        {
            AccountId = account.Id,
            Person = new()
            {
                Id = person.Id,
                Class = person.Class,
                Level = person.Level,
                Name = person.Name,
                Photo = person.Photo,
            }
        }, ct);
    }
}
