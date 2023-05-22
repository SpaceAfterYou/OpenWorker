namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

public interface IAuthService
{
    ValueTask JoinAsync(int account, ushort gate, long key, CancellationToken ct = default);
}
