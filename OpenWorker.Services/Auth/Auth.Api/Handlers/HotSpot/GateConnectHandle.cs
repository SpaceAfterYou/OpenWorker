using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class GateConnectHandle : IHotSpotHandler<LoginServerConnectServerMessage>
{
    private readonly IGateService _service;

    public GateConnectHandle(IGateService service) => _service = service;

    public async ValueTask OnHandleAsync(Entity entity, LoginServerConnectServerMessage request, CancellationToken ct)
    {
        if (!entity.Has<AccountComponent>()) return;
        
        await _service.JoinAsync(request.Gate, ct);
    }
}
