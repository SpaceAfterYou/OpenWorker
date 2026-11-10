using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Items.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// What limits the use of items in time. Every item belongs to a cooldown group with a time (<c>CooldownMilliseconds</c>): after
/// one is used no item of the group can be used for that long, counted in whole seconds as the game does. A group an item marks
/// as saved keeps running when the person changes server or leaves; the others end with the server. A few kinds of item (the
/// items of fatigue) are also limited to <see cref="DailyUses"/> a game day, and the count of every kind is told to the client
/// when the person enters (08/48) and when the day turns and it starts again (08/49).
/// </summary>
public sealed class ItemUseLimits(
    World world,
    PersonStateStore store,
    TimeProvider time,
    IOptions<DailyMissionOptions> options) : IMiscTick
{
    /// <summary>
    /// How often a limited kind of item may be used on a game day.
    /// </summary>
    public const int DailyUses = 3;

    private static QueryDescription Persons { get; } = new QueryDescription().WithAll<ItemUseComponent, ServerSessionComponent>();

    private TimeSpan Reset => options.Value.ResetHour is { } hour ? TimeSpan.FromHours(hour) : DailyMissionRules.DefaultResetTime;

    private long Now => time.GetUtcNow().ToUnixTimeSeconds();

    /// <summary>
    /// Loads the book of the person as it enters a server, lets what ran out go, starts a new day where one has passed and
    /// tells the client the counts.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<ItemUseBook>(person, PersonStateKind.ItemUse, cancellationToken) ?? new ItemUseBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        var now = Now;

        book.Saved.RemoveAll(e => e.EndsAt <= now);
        StartDay(book, now);

        world.Set(player, new ItemUseComponent { Person = person, Book = book });

        SendList(player, book);

        // A person who opened no place is told nothing, so the client keeps its own idea of the places.
        if (book.OpenEquipSlots != 0 && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new ItemEquipSlotOpenResponse { OpenMask = book.OpenEquipSlots });
        }
    }

    /// <summary>
    /// Whether the place of the equipment is open: one bit of the mask for each of the memory places, from the first.
    /// </summary>
    public bool IsEquipSlotOpen(Entity player, int bit)
    {
        return BookOf(player, create: false) is { } book && (book.OpenEquipSlots & (1 << bit)) != 0;
    }

    /// <summary>
    /// Opens a place of the equipment for good and tells the client which places are open.
    /// </summary>
    public void OpenEquipSlot(Entity player, int bit)
    {
        if (BookOf(player, create: true) is not { } book)
        {
            return;
        }

        book.OpenEquipSlots |= 1 << bit;
        world.MarkDirty(player, PersonSections.MiscState);

        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new ItemEquipSlotOpenResponse { OpenMask = book.OpenEquipSlots });
        }
    }

    /// <summary>
    /// Whether an item's cooldown group is still running.
    /// </summary>
    public bool IsCooling(Entity player, in ItemRow item)
    {
        return BookOf(player, create: false) is { } book && EndOf(book, item) > Now;
    }

    /// <summary>
    /// Starts the cooldown of an item's group. An item with no time starts nothing.
    /// </summary>
    public void StartCooldown(Entity player, in ItemRow item)
    {
        if (item.CooldownMilliseconds <= 0 || BookOf(player, create: true) is not { } book)
        {
            return;
        }

        var ends = Now + item.CooldownMilliseconds / 1000;
        var group = item.CooldownGroup;

        if (item.CooldownSaved == 0)
        {
            book.Running[group] = ends;

            return;
        }

        var saved = book.Saved.Find(e => e.Group == group);

        if (saved is null)
        {
            book.Saved.Add(new SavedCooldown { Group = group, EndsAt = ends });
        }
        else
        {
            saved.EndsAt = ends;
        }

        world.MarkDirty(player, PersonSections.MiscState);
    }

    /// <summary>
    /// Whether a kind of item may still be used today: a kind not used yet may, one used fewer than <see cref="DailyUses"/> times may.
    /// </summary>
    public bool CanUseKind(Entity player, int type)
    {
        if (BookOf(player, create: false) is not { } book)
        {
            return true;
        }

        StartDay(book, Now);

        return book.Uses.Find(e => e.Type == type) is not { } entry || entry.Count < DailyUses;
    }

    /// <summary>
    /// Counts a use of the kind today.
    /// </summary>
    public void CountUse(Entity player, int type)
    {
        if (BookOf(player, create: true) is not { } book)
        {
            return;
        }

        var now = Now;

        StartDay(book, now);

        var entry = book.Uses.Find(e => e.Type == type);

        if (entry is null)
        {
            book.Uses.Add(new ItemUseCount { Type = type, Count = 1, UsedAt = now });
        }
        else
        {
            entry.Count = (byte)Math.Min(byte.MaxValue, entry.Count + 1);
            entry.UsedAt = now;
        }

        book.UpdatedAt = now;
        world.MarkDirty(player, PersonSections.MiscState);
    }

    /// <summary>
    /// How often the person used the kind today.
    /// </summary>
    public int UsesOf(Entity player, int type)
    {
        return BookOf(player, create: false) is { } book ? book.Uses.Find(e => e.Type == type)?.Count ?? 0 : 0;
    }

    /// <summary>
    /// Starts the new day for every person whose counts belong to an earlier one.
    /// </summary>
    public ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        world.Query(Persons, entity => players.Add(entity));

        var now = Now;

        foreach (var player in players)
        {
            if (world.IsAlive(player) && BookOf(player, create: false) is { } book && StartDay(book, now))
            {
                world.MarkDirty(player, PersonSections.MiscState);
                world.Get<ServerSessionComponent>(player).Send(new ItemUseInfoUpdateResponse { List = book.Uses.Select(e => e.ToValue()).ToArray() });
            }
        }

        return ValueTask.CompletedTask;
    }

    private void SendList(Entity player, ItemUseBook book)
    {
        if (world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new ItemUseInfoListResponse
            {
                UpdatedAt = book.UpdatedAt,
                List = book.Uses.Select(e => e.ToValue()).ToArray()
            });
        }
    }

    /// <summary>
    /// Sets the counts of kinds last used before the game day that is running back to nothing; true when one changed.
    /// </summary>
    private bool StartDay(ItemUseBook book, long now)
    {
        var day = GameDay.Get(DateTimeOffset.FromUnixTimeSeconds(now), Reset).Start.ToUnixTimeSeconds();
        var changed = false;

        foreach (var entry in book.Uses.Where(e => e.UsedAt < day && (e.Count != 0 || e.UsedAt != 0)))
        {
            entry.Count = 0;
            entry.UsedAt = 0;
            changed = true;
        }

        if (changed)
        {
            book.UpdatedAt = now;
        }

        return changed;
    }

    private static long EndOf(ItemUseBook book, in ItemRow item)
    {
        var group = item.CooldownGroup;

        if (item.CooldownSaved != 0)
        {
            return book.Saved.Find(e => e.Group == group)?.EndsAt ?? 0;
        }

        return book.Running.GetValueOrDefault(group);
    }

    private ItemUseBook? BookOf(Entity player, bool create)
    {
        if (!world.IsAlive(player))
        {
            return null;
        }

        if (world.TryGet<ItemUseComponent>(player, out var component) && component.Book is { } book)
        {
            return book;
        }

        if (!create)
        {
            return null;
        }

        // A person nobody loaded a book for (a test, a command): it gets an empty one, which is written like any.
        book = new ItemUseBook();

        if (world.Has<ItemUseComponent>(player))
        {
            world.Set(player, new ItemUseComponent { Book = book });
        }
        else
        {
            world.Add(player, new ItemUseComponent { Book = book });
        }

        return book;
    }
}
