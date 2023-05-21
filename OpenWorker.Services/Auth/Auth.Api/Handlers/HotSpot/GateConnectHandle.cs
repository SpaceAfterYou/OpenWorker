using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Components;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class GateConnectHandle : IHotSpotHandler<LoginServerConnectServerMessage>
{
    public async ValueTask OnHandleAsync(Entity entity, LoginServerConnectServerMessage request, CancellationToken ct)
    {
        if (entity.Has<AuthComponent>()) return;

        var gate = entity.Get<GateComponent>();
        await gate.JoinAsync(request.Gate, ct);
    }
}
