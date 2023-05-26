namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IRouletteService
{
    ValueTask SendCurrentState(CancellationToken ct = default);
}
