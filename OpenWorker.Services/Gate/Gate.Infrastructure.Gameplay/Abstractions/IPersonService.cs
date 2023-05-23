namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

public interface IPersonService
{
    ValueTask ShowList(CancellationToken ct = default);
    ValueTask Delete(int id, CancellationToken ct = default);
    ValueTask SwapSlot(byte left, byte right, CancellationToken ct = default);
    ValueTask Select(int id, CancellationToken ct = default);
}
