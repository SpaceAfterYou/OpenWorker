namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

public interface IAccountService
{
    ValueTask ChangeBackgroundAsync(int id, CancellationToken ct = default);
    ValueTask ChangeRepPersonAsync(int id, CancellationToken ct = default);
}
