using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;
using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Service.Abstractions;

namespace OpenWorker.Services.Gate.Api.Handlers.HotSpot.Auth;

public sealed record JoinHandler : IHotSpotHandler<LoginEnterServerMessage>
{
    private readonly IAuthService _authService;
    private readonly IServerService _serverService;

    public JoinHandler(IAuthService service, IServerService serverService)
    {
        _authService = service;
        _serverService = serverService;
    }

    public async ValueTask OnHandleAsync(Entity entity, LoginEnterServerMessage request, CancellationToken ct)
    {
        if (!entity.Has<AccountComponent>())
        {
            return;
        }

        await _authService.JoinAsync(request.Account, request.LastGate, request.Key, ct);
        await _serverService.SyncDateTime(ct);
    }
}
