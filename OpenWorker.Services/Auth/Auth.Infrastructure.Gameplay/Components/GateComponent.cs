using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Components;

public sealed record GateComponent
{
    private readonly IGameplayGate _gateList;
    private readonly IHotSpotSession _session;

    public GateComponent(IGameplayGate gateList, IHotSpotSession session)
    {
        _gateList = gateList;
        _session = session;
    }

    public ValueTask<bool> JoinAsync(ushort id, CancellationToken ct = default) => _gateList
        .Join(_session, id, ct);

    public ValueTask ShowAvailableGatesAsync(CancellationToken ct = default) => _gateList
        .ShowAvailableGates(_session.Entity, _session, ct);

    public ValueTask UpdateClientFeaturesAsync(CancellationToken ct = default) => _gateList
        .UpdateClientFeatures(_session.Entity, _session, ct);
}
