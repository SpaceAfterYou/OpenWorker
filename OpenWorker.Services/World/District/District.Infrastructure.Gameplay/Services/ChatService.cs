using Microsoft.EntityFrameworkCore;
using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Database;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.Defines;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services;

internal sealed record ChatService : IChatService
{
    private readonly IHotSpotSession _session;
    private readonly IDbContextFactory<PersistentContext> _factory;

    public ChatService(IDbContextFactory<PersistentContext> factory, IHotSpotSession session)
    {
        _session = session;
        _factory = factory;
    }

    public async ValueTask BroadcastNormal(ChatType type, string message, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (await db.ChattingFilter.AnyAsync(e => message.Contains(e.Field0), ct) is true)
        {
            return;
        }

        // db.ChattingCommand

        var person = _session.Entity.Get<PersonComponent>();
        await _session.Entity.Get<InstanceChannel>().BroadcastChat(person.Id, type, message, ct);
    }
}
