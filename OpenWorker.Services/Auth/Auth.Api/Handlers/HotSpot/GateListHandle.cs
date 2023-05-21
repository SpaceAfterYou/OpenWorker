using DefaultEcs;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot;

internal sealed class GateListHandle : IHotSpotHandler<LoginServerListServerMessage>
{
    private readonly IGateService _service;
    private readonly ILogger _logger;

    public GateListHandle(IGateService service, ILogger<GateListHandle> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async ValueTask OnHandleAsync(Entity entity, LoginServerListServerMessage request, CancellationToken ct)
    {
        if (!entity.Has<AccountComponent>()) return;

        var account = entity.Get<AccountComponent>();
        if (request.AccountId != account.Id)
        {
            _logger.LogWarning("Bad account. ID={} / REQUESTED={}", account.Id, request.AccountId);
            return;
        }

        await _service.ShowAvailableGatesAsync(ct);
        await _service.ShowEnabledFeaturesAsync(ct);
    }
}
