using OpenWorker.Infrastructure.Gameplay.Realm.Components;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

public interface IAuthService
{
    ValueTask<AccountComponent?> JoinAsync(string login, string password, string mac, CancellationToken ct = default);
}
