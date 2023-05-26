using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Character;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IPersonService
{
    ValueTask SyncState(CancellationToken ct = default);
    ValueTask LoadPerson(CancellationToken ct = default);
    ValueTask SendCurrent(CancellationToken ct = default);
}

internal sealed record PersonService : IPersonService
{
    private readonly IHotSpotSession _session;

    public async ValueTask SyncState(CancellationToken ct = default)
    {
        await _session.SendAsync(new CharacterDatabaseLoadSyncClientMessage(), ct);
    }

    public async ValueTask LoadPerson(CancellationToken ct = default)
    {
        // load person
    }

    public async ValueTask SendCurrent(CancellationToken ct = default)
    {
    }
}
