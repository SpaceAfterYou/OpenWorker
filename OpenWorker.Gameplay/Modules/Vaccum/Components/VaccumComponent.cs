using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Vaccum;

/// <summary>
/// The cubes of a person and what it is taking now.
/// </summary>
[EntityComponent(EntityComponentService.District)]
[EntityComponent(EntityComponentService.Maze)]
public struct VaccumComponent
{
    public Dictionary<int, VaccumCubeState>? Cubes;

    /// <summary>
    /// The cube being taken, 0 for none.
    /// </summary>
    public int Taking;

    /// <summary>
    /// Changes whenever a take starts or stops, so a wait that belongs to an earlier take finds out.
    /// </summary>
    public int Ticket;
}
