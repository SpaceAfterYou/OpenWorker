using Arch.Core;

namespace OpenWorker.GameServer.Servers.WorldServer;

public interface IMazeService
{
    void TakeDamage(Entity entity, int damage, int creatureId);
}