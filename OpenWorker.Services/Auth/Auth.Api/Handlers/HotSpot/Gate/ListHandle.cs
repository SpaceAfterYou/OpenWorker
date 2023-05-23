using DefaultEcs;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;

namespace OpenWorker.Services.Auth.Api.Handlers.HotSpot.Gate;

internal sealed record ListHandle : IHotSpotHandler<LoginServerListServerMessage>
{
    private readonly IGateService _service;
    private readonly ILogger _logger;

    public ListHandle(IGateService service, ILogger<ListHandle> logger)
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
