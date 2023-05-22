namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;

public interface IAccountService
{
    ValueTask ChangeBackground(int id, CancellationToken ct = default);
    ValueTask ChangeRepresentativePerson(int id, CancellationToken ct = default);
}
