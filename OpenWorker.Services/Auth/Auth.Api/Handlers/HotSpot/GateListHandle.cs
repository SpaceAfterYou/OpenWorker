using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Components;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class GateListHandle : IHotSpotHandler<LoginServerListServerMessage>
{
    private readonly ILogger _logger;

    public GateListHandle(ILogger<GateListHandle> logger) => _logger = logger;

    public async ValueTask OnHandleAsync(Entity entity, LoginServerListServerMessage request, CancellationToken ct)
    {
        if (entity.Has<AuthComponent>()) return;

        var account = entity.Get<AccountComponent>();
        if (request.AccountId != account.Id)
        {
            _logger.LogWarning("Bad account. ID={} / REQUESTED={}", account.Id, request.AccountId);
            return;
        }

        var gate = entity.Get<GateComponent>();

        await gate.ShowAvailableGatesAsync(ct);
        await gate.UpdateClientFeaturesAsync(ct);
    }
}
