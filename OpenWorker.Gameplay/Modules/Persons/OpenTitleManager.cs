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
/// person has just done: an achievement it reached (<see cref="ByAchievement"/>), or the level it came to
/// (<see cref="ByLevel"/>). A prefix and a suffix may open at once. A title the person has earned is not listed as open. A
/// change marks <see cref="PersonSections.Progress"/> and the flush writes the book.
/// </summary>
public sealed class OpenTitleManager(World world, PersonStateStore store, TitleCatalog catalog)
{
    /// <summary>The title opens with an achievement; the value is the number of the achievement.</summary>
    public const byte ByAchievement = 2;

    /// <summary>The title opens with a level; the value is the level.</summary>
    public const byte ByLevel = 3;

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
    /// The person has done what the rows of titles may ask for: opens the prefix and the suffix that ask for exactly this,
    /// unless they are open already or the person has them. Each that opens is told.
    /// </summary>
    public void Update(Entity player, byte condition, int value)
    {
        if (!world.IsAlive(player) || !world.TryGet<OpenTitleComponent>(player, out var component) || component.Book is not { } book ||
            !world.TryGet<PersonInfoComponent>(player, out var info))
        {
            return;
        }

        var owned = world.TryGet<TitleComponent>(player, out var titles) ? titles.Owned ?? [] : [];

        foreach (var title in catalog.OpenedBy(condition, value, info.Hero))
        {
            if (book.Titles.Contains(title) || owned.Contains(title))
            {
                continue;
            }

            book.Titles.Add(title);

            world.MarkDirty(player, PersonSections.Progress);

            if (world.TryGet<ServerSessionComponent>(player, out var session))
            {
                session.Send(new PersonUpdateOpenTitleResponse { Title = title });
            }
        }
    }
}
