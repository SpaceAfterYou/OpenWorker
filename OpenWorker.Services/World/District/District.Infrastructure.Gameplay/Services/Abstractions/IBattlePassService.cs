namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IBattlePassService
{
    ValueTask SendCurrentState(CancellationToken ct = default);
}
