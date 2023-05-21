using SoulWorkerResearch.SoulCore.IO.Net.Messages.Client.Login;

namespace OpenWorker.Services.Auth.Infrastructure.Gameplay.Abstractions;

public interface IGateService
{
    IAsyncEnumerable<LoginUserPersonsForServerResponseClientMessage.Entry> GetAvailableGates(CancellationToken ct = default);

    ValueTask<bool> JoinAsync(ushort id, CancellationToken ct = default);
    ValueTask ShowAvailableGatesAsync(CancellationToken ct = default);
    ValueTask ShowEnabledFeaturesAsync(CancellationToken ct = default);
}
