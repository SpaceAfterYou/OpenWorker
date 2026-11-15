using Arch.Core;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Modules.Persons;

/// <summary>
/// The titles that have become visible in the list of a person, without being its own yet.
/// </summary>
public sealed class OpenTitleBook
{
    public List<int> Titles { get; set; } = [];
}

/// <summary>
/// A title opens, which shows it in the list of the client with what is needed to earn it, when its row asks for what the
/// person has just done: an achievement it reached (<see cref="ByAchievement"/>), the level it came to
/// (<see cref="ByLevel"/>), a monster it killed (<see cref="ByMonsterKill"/>), or an event a game master names
/// (<see cref="ByEvent"/>). A prefix and a suffix may open at once. A title the person has earned is not listed as open. A
/// change marks <see cref="PersonSections.Progress"/> and the flush writes the book.
/// </summary>
public sealed class OpenTitleManager(World world, PersonStateStore store, TitleCatalog catalog)
{
    /// <summary>The title opens with an achievement; the value is the number of the achievement.</summary>
    public const byte ByAchievement = 2;

    /// <summary>The title opens with a level; the value is the level.</summary>
    public const byte ByLevel = 3;

    /// <summary>The title opens when the person kills a monster; the value is the monster.</summary>
    public const byte ByMonsterKill = 6;

    /// <summary>The title opens with an event; only a game master opens it.</summary>
    public const byte ByEvent = 9;

    /// <summary>
    /// The book of the open titles as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<OpenTitleComponent>(PersonStateKind.OpenTitles, PersonSections.Progress, e => e.Book);

    /// <summary>
    /// Loads the open titles of the person. The list is told with the titles when the client asks for them.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<OpenTitleBook>(person, PersonStateKind.OpenTitles, cancellationToken) ?? new OpenTitleBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        book.Titles = [.. book.Titles.Distinct().Where(e => catalog.TryGet(e, out _))];

        world.Set(player, new OpenTitleComponent { Person = person, Book = book });
    }

    /// <summary>
    /// The titles that are open and not earned, in the order they opened.
    /// </summary>
    public IReadOnlyList<int> OpenOf(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet<OpenTitleComponent>(player, out var component) || component.Book is not { } book)
        {
            return [];
        }

        var owned = world.TryGet<TitleComponent>(player, out var titles) ? titles.Owned ?? [] : [];

        return [.. book.Titles.Where(e => !owned.Contains(e))];
    }

    /// <summary>
    /// Opens, without telling, the titles of what the person reached before it came in: its achievements and every level up
    /// to its own. Titles opened this way are listed with the others when the client asks.
    /// </summary>
    public void CatchUp(Entity player, IEnumerable<int> achievements, int level)
    {
        foreach (var achievement in achievements)
        {
            Update(player, ByAchievement, achievement, tell: false);
        }

        for (var reached = 1; reached <= level; reached++)
        {
            Update(player, ByLevel, reached, tell: false);
        }
    }

    /// <summary>
    /// The person has done what the rows of titles may ask for: opens the prefix and the suffix that ask for exactly this,
    /// unless they are open already or the person has them. Each that opens is told. Returns how many opened.
    /// </summary>
    public int Update(Entity player, byte condition, int value) => Update(player, condition, value, tell: true);

    private int Update(Entity player, byte condition, int value, bool tell)
    {
        if (!world.IsAlive(player) || !world.TryGet<OpenTitleComponent>(player, out var component) || component.Book is not { } book ||
            !world.TryGet<PersonInfoComponent>(player, out var info))
        {
            return 0;
        }

        var opened = 0;

        var owned = world.TryGet<TitleComponent>(player, out var titles) ? titles.Owned ?? [] : [];

        foreach (var title in catalog.OpenedBy(condition, value, info.Hero))
        {
            if (book.Titles.Contains(title) || owned.Contains(title))
            {
                continue;
            }

            book.Titles.Add(title);
            opened++;

            world.MarkDirty(player, PersonSections.Progress);

            if (tell && world.TryGet<ServerSessionComponent>(player, out var session))
            {
                session.Send(new PersonUpdateOpenTitleResponse { Title = title });
            }
        }

        return opened;
    }
}
