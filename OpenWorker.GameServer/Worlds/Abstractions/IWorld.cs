namespace OpenWorker.GameServer.Worlds.Abstractions;

public interface IWorld
{
    int TickRate { get; }

    Task TickAsync(double deltaTime, CancellationToken cancellationToken);
}