using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Responses;
using OpenWorker.Hotspot.Modules.Boosters.Types;

namespace OpenWorker.Gameplay.Modules.Boosters;

/// <summary>
/// Boosters change what a person earns or spends for a while. They come from items, events and parties, last for
/// a time that is used up in districts, in mazes or everywhere, and only one of a group can be active.
/// </summary>
public sealed class BoosterManager(
    World world,
    PersonStateStore store,
    BoosterCatalog catalog,
    TimeProvider time,
    ILogger<BoosterManager> logger) : IMiscTick
{
    private static readonly QueryDescription Everyone = new QueryDescription().WithAll<BoosterComponent>();

    /// <summary>
    /// The longest time that is taken between two looks, so a pause of the server does not use boosters up.
    /// </summary>
    private static readonly TimeSpan LongestStep = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Loads the boosters of the person that are still running and tells the client about them.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, BoosterConsumeArea area, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<BoosterBook>(person, PersonStateKind.Booster, cancellationToken) ?? new BoosterBook();

        var now = time.GetUtcNow();
        var before = book.Boosters.Count;

        book.Boosters.RemoveAll(e => !catalog.TryGet(e.Booster, out var definition) || BoosterRules.IsOver(definition, e, now.ToUnixTimeSeconds()));

        world.Set(player, new BoosterComponent { Person = person, Book = book, Area = area, LastTick = now });

        SendList(player, book, area, now);

        if (book.Boosters.Count != before)
        {
            await SaveAsync(player, book, cancellationToken);
        }
    }

    /// <summary>
    /// Gives the person a booster. When it already has the booster, its time grows; a booster of the same group
    /// takes the place of the one that was active. The time is that of the table unless one is given.
    /// </summary>
    public async ValueTask<bool> AddAsync(Entity player, short booster, TimeSpan? duration, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component) || !catalog.TryGet(booster, out var definition))
        {
            return false;
        }

        var milliseconds = (long)(duration?.TotalMilliseconds ?? definition.DurationMilliseconds);

        // A booster that runs out needs a time to run for.
        if (definition.Time != BoosterTime.Endless && milliseconds <= 0)
        {
            logger.LogDebug("The booster {Booster} has no time to run for.", booster);
            return false;
        }

        var book = component.Book!;
        var session = world.Get<ServerSessionComponent>(player);
        var now = time.GetUtcNow();

        foreach (var other in book.Boosters.Where(e => e.Booster != booster && catalog.TryGet(e.Booster, out var existing) && existing.Group == definition.Group).ToArray())
        {
            book.Boosters.Remove(other);
            session.Send(new BoosterRemoveResponse { Id = other.Booster });
        }

        var entry = book.Find(booster);

        if (entry is null)
        {
            entry = new BoosterEntry { Booster = booster };
            book.Boosters.Add(entry);
        }

        BoosterRules.Charge(definition, entry, milliseconds, now.ToUnixTimeSeconds());

        session.Send(new BoosterAddResponse
        {
            Area = component.Area,
            Identifier = booster,
            RemainSeconds = BoosterRules.GetRemainSeconds(definition, entry, now.ToUnixTimeSeconds())
        });

        await SaveAsync(player, book, cancellationToken);

        return true;
    }

    public async ValueTask<bool> RemoveAsync(Entity player, short booster, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component) || component.Book!.Find(booster) is not { } entry)
        {
            return false;
        }

        component.Book.Boosters.Remove(entry);

        world.Get<ServerSessionComponent>(player)
            .Send(new BoosterRemoveResponse { Id = booster });

        await SaveAsync(player, component.Book, cancellationToken);

        return true;
    }

    /// <summary>
    /// Takes all the boosters of the person away.
    /// </summary>
    public async ValueTask ClearAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component))
        {
            return;
        }

        component.Book!.Boosters.Clear();

        world.Get<ServerSessionComponent>(player)
            .Send(new BoosterClearResponse());

        await SaveAsync(player, component.Book, cancellationToken);
    }

    /// <summary>
    /// The person moves to the other kind of place, which decides whose time goes.
    /// </summary>
    public void SetArea(Entity player, BoosterConsumeArea area)
    {
        if (TryGet(player, out var component))
        {
            world.Set(player, component with { Area = area });
        }
    }

    /// <summary>
    /// What the boosters of the person do to an amount, for the features that pay out or charge something.
    /// </summary>
    public float Apply(Entity player, BoosterEffect effect, float origin)
    {
        return TryGet(player, out var component)
            ? BoosterRules.Apply(GetDefinitions(component.Book!), effect, origin)
            : origin;
    }

    /// <summary>
    /// The part of an amount that the boosters of the person add, such as 0.2 for a fifth more.
    /// </summary>
    public float GetRate(Entity player, BoosterEffect effect)
    {
        return TryGet(player, out var component) ? BoosterRules.GetRate(GetDefinitions(component.Book!), effect) : 0;
    }

    /// <summary>
    /// The amount the boosters of the person add.
    /// </summary>
    public float GetValue(Entity player, BoosterEffect effect)
    {
        return TryGet(player, out var component) ? BoosterRules.GetValue(GetDefinitions(component.Book!), effect) : 0;
    }

    /// <summary>
    /// Takes the time spent from the boosters and ends the ones that have run out.
    /// </summary>
    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        world.Query(in Everyone, (Entity entity, ref BoosterComponent component) =>
        {
            if (component.Book is not null)
            {
                players.Add(entity);
            }
        });

        var now = time.GetUtcNow();

        foreach (var player in players)
        {
            try
            {
                await TickPersonAsync(player, now, cancellationToken);
            }

            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Boosters of a person failed.");
            }
        }
    }

    private async ValueTask TickPersonAsync(Entity player, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var component))
        {
            return;
        }

        var book = component.Book!;
        var elapsed = now - component.LastTick;

        world.Set(player, component with { LastTick = now });

        var milliseconds = (long)Math.Min(Math.Max(elapsed.TotalMilliseconds, 0), LongestStep.TotalMilliseconds);
        var changed = false;
        var session = world.Get<ServerSessionComponent>(player);

        foreach (var entry in book.Boosters.ToArray())
        {
            if (!catalog.TryGet(entry.Booster, out var definition))
            {
                continue;
            }

            changed |= BoosterRules.Consume(definition, entry, component.Area, milliseconds);

            if (BoosterRules.IsOver(definition, entry, now.ToUnixTimeSeconds()))
            {
                book.Boosters.Remove(entry);
                session.Send(new BoosterRemoveResponse { Id = entry.Booster });
                changed = true;
            }
        }

        if (changed)
        {
            await SaveAsync(player, book, cancellationToken);
        }
    }

    private void SendList(Entity player, BoosterBook book, BoosterConsumeArea area, DateTimeOffset now)
    {
        var entries = new List<BoosterLoadEntry>();

        foreach (var entry in book.Boosters)
        {
            if (catalog.TryGet(entry.Booster, out var definition))
            {
                entries.Add(new BoosterLoadEntry(entry.Booster, BoosterRules.GetRemainSeconds(definition, entry, now.ToUnixTimeSeconds())));
            }
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new BoosterLoadResponse { Area = area, Entries = entries });
    }

    private List<BoosterDefinition> GetDefinitions(BoosterBook book)
    {
        var result = new List<BoosterDefinition>();

        foreach (var entry in book.Boosters)
        {
            if (catalog.TryGet(entry.Booster, out var definition))
            {
                result.Add(definition);
            }
        }

        return result;
    }

    private bool TryGet(Entity player, out BoosterComponent component)
    {
        if (world.IsAlive(player) && world.TryGet(player, out component) && component.Book is not null)
        {
            return true;
        }

        component = default;
        return false;
    }

    private async ValueTask SaveAsync(Entity player, BoosterBook book, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var person = world.Get<BoosterComponent>(player).Person;

        await book.Gate.WaitAsync(cancellationToken);

        try
        {
            await store.SaveAsync(person, PersonStateKind.Booster, book, cancellationToken);
        }

        finally
        {
            book.Gate.Release();
        }
    }
}
