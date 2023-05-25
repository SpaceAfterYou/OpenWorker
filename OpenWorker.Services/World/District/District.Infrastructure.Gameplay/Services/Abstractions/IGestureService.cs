using System.Numerics;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IGestureService
{
    ValueTask Show(int id, Vector3 position, Vector2 rotation, CancellationToken ct = default);
    ValueTask ChangeSlot(IEnumerable<int> values, CancellationToken ct = default);
}
