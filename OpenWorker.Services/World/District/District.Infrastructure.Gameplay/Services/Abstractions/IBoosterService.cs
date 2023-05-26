namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IBoosterService
{
    ValueTask SendDaily(CancellationToken ct = default);
}
