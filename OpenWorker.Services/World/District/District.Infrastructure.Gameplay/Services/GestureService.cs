using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.DatabaseModel.Resources;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.District.Infrastructure.Gameplay.Extensions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using Redis.OM;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Bidirectional.Gesture;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Gesture;
using System.Numerics;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed record GestureService : IGestureService
{
    private readonly IHotSpotSession _session;
    private readonly IDbContextFactory<PersistentContext> _factory;

    private readonly ILogger<GestureService> _logger;

    public GestureService(IHotSpotSession session, IDbContextFactory<PersistentContext> factory, ILogger<GestureService> logger)
    {
        _session = session;
        _factory = factory;

        _logger = logger;
    }

    public async ValueTask ChangeSlot(IEnumerable<int> values, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        foreach (var id in values)
        {
            if (await db.Gesture.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct) is not GestureResource gesture)
            {
                _logger.LogWarning("Bad gesture. ID={}, ACCOUNT ID={}", id, _session.Entity.Get<AccountComponent>().Id);
                return;
            }

            var person = _session.Entity.Get<PersonComponent>();
            if (gesture.IsValid(person.Class) is false)
            {
                _logger.LogWarning("Bad gesture class. ID={}, ACCOUNT ID={}", id, _session.Entity.Get<AccountComponent>().Id);
                return;
            }
        }

        await _session.SendAsync(new GestureSlotUpdateBidirectionalMessage { Values = values, }, ct);
    }

    public async ValueTask Show(int id, Vector3 position, Vector2 rotation, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.Gesture.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct) is not GestureResource gesture)
        {
            _logger.LogWarning("Bad gesture. ID={}, ACCOUNT ID={}", id, _session.Entity.Get<AccountComponent>().Id);
            return;
        }

        var person = _session.Entity.Get<PersonComponent>();
        if (gesture.IsValid(person.Class) is false)
        {
            _logger.LogWarning("Bad gesture class. ID={}, ACCOUNT ID={}", id, _session.Entity.Get<AccountComponent>().Id);
            return;
        }

        await _session.Entity.Get<InstanceChannel>().BroadcastGesture(id, person.Id, position, rotation, ct);
    }
}
