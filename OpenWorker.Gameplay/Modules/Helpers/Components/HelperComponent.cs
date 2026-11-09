using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Helpers.Components;

/// <summary>
/// The helpers of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct HelperComponent
{
    public int Person;

    public HelperBook? Book;

    /// <summary>
    /// The person is in a maze, the one place a helper can be called to.
    /// </summary>
    public bool InMaze;
}
