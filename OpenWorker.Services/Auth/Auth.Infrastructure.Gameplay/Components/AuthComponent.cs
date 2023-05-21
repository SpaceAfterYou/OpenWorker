using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Components;

public sealed record AuthComponent
{
    private readonly IHotSpotSession _session;
    private readonly IGameplayAuth _gameplay;

    public AuthComponent(IHotSpotSession session, IGameplayAuth gameplay)
    {
        _session = session;
        _gameplay = gameplay;
    }

    public ValueTask<AccountComponent?> LoginAsync(string login, string password, string mac, CancellationToken ct = default) => _gameplay
        .Login(_session, login, password, mac, ct);
}
