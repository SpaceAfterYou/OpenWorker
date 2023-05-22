namespace Gate.Infrastructure.Gameplay.Abstractions;

public interface IPersonService
{
    ValueTask Delete(int id, CancellationToken ct = default);
    ValueTask Join(int id, CancellationToken ct = default);
}
