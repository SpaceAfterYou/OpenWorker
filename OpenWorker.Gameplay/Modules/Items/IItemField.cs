using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Where a person stands when it uses an item, as far as the rules of items care: a city or a maze.
/// </summary>
/// <param name="InMaze">The person is in a maze.</param>
/// <param name="ItemsAllowed">The maze lets its players use items (a few mazes forbid potions).</param>
/// <param name="ModeAllowsItems">The way the maze is played lets items be used at all.</param>
/// <param name="Down">The person has fallen and has not got up.</param>
/// <param name="Infinite">The maze is the infinite tower.</param>
public readonly record struct ItemPlace(bool InMaze, bool ItemsAllowed, bool ModeAllowsItems, bool Down, bool Infinite)
{
    /// <summary>
    /// A city: it takes items, and nobody is down. What a kind of item may do there is told by its use state.
    /// </summary>
    public static ItemPlace District { get; } = new(false, true, true, false, false);
}

/// <summary>
/// The place items act in: the maze, where a potion gives its buff to the person. A server without mazes answers as a city does.
/// </summary>
public interface IItemField
{
    /// <summary>
    /// Where the person stands.
    /// </summary>
    ItemPlace PlaceOf(Entity player);

    /// <summary>
    /// Gives the person a buff for sure, as an item does; false when the person cannot carry it now.
    /// </summary>
    bool GiveBuff(Entity player, int buff);
}

/// <summary>
/// The field of a server with no maze.
/// </summary>
public sealed class DistrictItemField : IItemField
{
    public ItemPlace PlaceOf(Entity player) => ItemPlace.District;

    public bool GiveBuff(Entity player, int buff) => false;
}

/// <summary>
/// Gives the titles that items give.
/// </summary>
public interface IItemTitles
{
    /// <summary>
    /// The person has the title already (or cannot have it): an item of the title is refused.
    /// </summary>
    bool CannotGrant(Entity player, int title);

    /// <summary>
    /// Gives the person the title and tells it.
    /// </summary>
    ValueTask<bool> GrantAsync(Entity player, int title);
}

/// <summary>
/// Used where nobody keeps titles: no item of a title can be used.
/// </summary>
public sealed class NoItemTitles : IItemTitles
{
    public bool CannotGrant(Entity player, int title) => true;

    public ValueTask<bool> GrantAsync(Entity player, int title) => ValueTask.FromResult(false);
}
