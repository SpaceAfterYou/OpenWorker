using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// A buff as the person took it away from a server: the buff, how long it had left, how many were stacked in it, and the actor
/// that gave it.
/// </summary>
public sealed class SavedBuff
{
    public ushort Buff { get; set; }

    /// <summary>Seconds it had left; zero for a buff that lasts.</summary>
    public float Time { get; set; }

    public byte Count { get; set; } = 1;

    public int Owner { get; set; }
}

/// <summary>
/// The buffs a person carries from one server to the next, and through a relog: what its book held when it left the last server.
/// The time that passes while the person is away does not count against them.
/// </summary>
public sealed class BuffMemoryBook
{
    public List<SavedBuff> Buffs { get; set; } = [];
}

/// <summary>
/// The buffs the person had when it came, null until they are loaded at the entry.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct BuffMemoryComponent
{
    public int Person { get; init; }

    public BuffMemoryBook? Book { get; init; }
}

public static class BuffMemoryDocument
{
    /// <summary>
    /// The memory as the flush of the person writes it.
    /// </summary>
    public static IPersonDocumentSource Source { get; } =
        new PersonDocumentSource<BuffMemoryComponent>(PersonStateKind.Buffs, PersonSections.MiscState, e => e.Book);
}

/// <summary>
/// Loads the buffs a person left the last server with, and writes down the ones it leaves this server with. The server that
/// carries buffs (the district for those that may be there, the maze for the rest) puts them back on the person and takes them
/// down again.
/// </summary>
public sealed class BuffMemory(World world, PersonStateStore store)
{
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<BuffMemoryBook>(person, PersonStateKind.Buffs, cancellationToken) ?? new BuffMemoryBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        Set(player, new BuffMemoryComponent { Person = person, Book = book });
    }

    /// <summary>
    /// The buffs the person came with, none when they are not loaded.
    /// </summary>
    public static IReadOnlyList<SavedBuff> Saved(World world, Entity player)
    {
        return world.IsAlive(player) && world.TryGet<BuffMemoryComponent>(player, out var memory) && memory.Book is { } book ? book.Buffs : [];
    }

    /// <summary>
    /// Writes down what the person carries now (but for the buffs that something gives again whenever the person comes: its
    /// passive skills, its clothes), to be written with the rest of the person.
    /// </summary>
    public static void Remember(World world, Entity player, BuffBook book, IReadOnlyCollection<ushort> given)
    {
        if (!world.IsAlive(player) || !world.TryGet<BuffMemoryComponent>(player, out var memory) || memory.Book is not { } saved)
        {
            return;
        }

        saved.Buffs = [.. book.Buffs.Where(e => !given.Contains(e.Buff)).Select(e => new SavedBuff
        {
            Buff = e.Buff,
            Time = e.Definition.Lasts ? 0f : Math.Max(0.1f, e.Remaining),
            Count = e.Count,
            Owner = e.Owner
        })];

        world.MarkDirty(player, PersonSections.MiscState);
    }

    private void Set(Entity player, in BuffMemoryComponent component)
    {
        if (world.Has<BuffMemoryComponent>(player))
        {
            world.Set(player, component);
        }
        else
        {
            world.Add(player, component);
        }
    }
}
