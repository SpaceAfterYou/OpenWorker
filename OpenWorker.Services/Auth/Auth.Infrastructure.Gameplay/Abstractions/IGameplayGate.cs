using OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;
using DefaultEcs;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

public interface IGameplayGate
{
    IAsyncEnumerable<LoginUserPersonsForServerResponseClientMessage.Entry> GetAvailableGates(Entity entity, CancellationToken ct = default);

    ValueTask<bool> Join(IHotSpotSession session, ushort id, CancellationToken ct = default);
    ValueTask ShowAvailableGates(Entity entity, IHotSpotSession session, CancellationToken ct = default);
    ValueTask UpdateClientFeatures(Entity entity, IHotSpotSession session, CancellationToken ct = default);
}
