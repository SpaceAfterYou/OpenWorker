using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Components;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class LoginHandler : IHotSpotHandler<LoginUserLoginServerMessage>
{
    public async ValueTask OnHandleAsync(Entity entity, LoginUserLoginServerMessage request, CancellationToken ct)
    {
        if (entity.Has<AccountComponent>()) return;

        if (await entity.Get<AuthComponent>().JoinAsync(request.Nickname, request.Password, request.Mac, ct) is AccountComponent account)
        {
            entity.Set(account);
            entity.Remove<AuthComponent>();
        }
    }
}
