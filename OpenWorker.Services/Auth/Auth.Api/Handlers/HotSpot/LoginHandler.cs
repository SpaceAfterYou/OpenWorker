using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class LoginHandler : IHotSpotHandler<LoginUserLoginServerMessage>
{
    private readonly IAuthService _service;

    public LoginHandler(IAuthService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, LoginUserLoginServerMessage request, CancellationToken ct)
    {
        if (entity.Has<AccountComponent>()) return;

        if (await _service.JoinAsync(request.Nickname, request.Password, request.Mac, ct) is AccountComponent account)
        {
            entity.Set(account);
        }
    }
}
