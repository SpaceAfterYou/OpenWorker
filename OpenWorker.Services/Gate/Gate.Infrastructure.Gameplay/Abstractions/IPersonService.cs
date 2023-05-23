namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

public interface IPersonService
{
    ValueTask ShowListAsync(CancellationToken ct = default);
    ValueTask DeleteAsync(int id, CancellationToken ct = default);
    ValueTask SwapSlotAsync(byte left, byte right, CancellationToken ct = default);
    ValueTask SelectAsync(int id, CancellationToken ct = default);
}
