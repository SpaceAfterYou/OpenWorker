namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IMazeService
{
    ValueTask SendCurrentLimits(CancellationToken ct = default);
}
