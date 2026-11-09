using Arch.Core;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// Which place of a player is kept. Only a district keeps one (<c>PersonPlaceStore</c>): a maze is entered and left
/// through reservations, and the next login starts where the person last stood in a district.
/// </summary>
public interface IPersonPlaceSource
{
    bool TryGetPlace(World world, Entity player, out PersonPlace place);
}
