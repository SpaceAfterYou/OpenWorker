using System.Text.Json;
using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// A progress document of a person (<see cref="PersonStateKind"/>) that the flush writes when its part is marked.
/// </summary>
public interface IPersonDocumentSource
{
    PersonSections Section { get; }

    PersonStateKind Kind { get; }

    /// <summary>
    /// The document as it is now, serialized on the world loop, or null when the player has none loaded.
    /// </summary>
    string? Snapshot(World world, Entity player);
}

/// <summary>
/// A document kept in a component of the player, such as the book of the quests.
/// </summary>
public sealed class PersonDocumentSource<TComponent>(PersonStateKind kind, PersonSections section, Func<TComponent, object?> document)
    : IPersonDocumentSource
{
    public PersonSections Section { get; } = section;

    public PersonStateKind Kind { get; } = kind;

    public string? Snapshot(World world, Entity player)
    {
        if (!world.TryGet<TComponent>(player, out var component) || document(component!) is not { } value)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, value.GetType(), PersonStateStore.Options);
    }
}
