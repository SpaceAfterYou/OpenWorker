using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

public interface IGameplayAuth
{
    ValueTask<AccountComponent?> Login(IHotSpotSession session, string login, string password, string mac, CancellationToken ct = default);
}
