using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// What of the person a connection plays has changed since the last flush. Null until the person has entered and its
/// state is loaded: nothing is written for a connection that never got that far.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct DirtyComponent(PersonDirty? Value);

/// <summary>
/// The dirty mask of one person. It is a reference, so a mark is never lost to a copy of the component read before an
/// <c>await</c>; it is read and changed on the world loop only.
/// </summary>
public sealed class PersonDirty(int person)
{
    public int Person { get; } = person;

    public PersonSections Sections { get; private set; }

    /// <summary>
    /// The place written last, so that a place that did not change is not written again.
    /// </summary>
    internal PersonPlace? WrittenPlace { get; set; }

    public void Mark(PersonSections sections)
    {
        Sections |= sections;
    }

    /// <summary>
    /// Hands over what is marked and starts a new round.
    /// </summary>
    internal PersonSections Take()
    {
        var sections = Sections;

        Sections = PersonSections.None;

        return sections;
    }
}

public static class DirtyExtensions
{
    /// <summary>
    /// Marks a part of the person as changed. A player whose state is not loaded (or an entity that is not a player)
    /// has nothing to write and is left alone.
    /// </summary>
    public static void MarkDirty(this World world, Entity player, PersonSections sections)
    {
        if (world.IsAlive(player) && world.TryGet<DirtyComponent>(player, out var dirty) && dirty.Value is { } mask)
        {
            mask.Mark(sections);
        }
    }

    public static PersonDirty? DirtyOf(this World world, Entity player)
    {
        return world.IsAlive(player) && world.TryGet<DirtyComponent>(player, out var dirty) ? dirty.Value : null;
    }
}
