namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IItemService
{
    ValueTask SendAkashicCurrentState(CancellationToken ct = default);
}
