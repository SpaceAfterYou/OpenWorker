using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;
using DefaultEcs;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

public interface IGateService
{
    IAsyncEnumerable<LoginUserPersonsForServerResponseClientMessage.Entry> GetAvailableGates(Entity entity, CancellationToken ct = default);

    ValueTask<bool> JoinAsync(IHotSpotSession session, ushort id, CancellationToken ct = default);
    ValueTask ShowAvailableGatesAsync(Entity entity, IHotSpotSession session, CancellationToken ct = default);
    ValueTask UpdateClientFeaturesAsync(Entity entity, IHotSpotSession session, CancellationToken ct = default);
}
