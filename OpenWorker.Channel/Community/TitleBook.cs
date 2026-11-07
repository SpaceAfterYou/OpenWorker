using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Where a title came from. The client is told with the title, and it is what the logs keep.
/// </summary>
public enum TitleSource
{
    Cheat = 0,
    Achievement = 1,
    Quest = 2,
    Item = 3,
    Echelon = 4
}

/// <summary>
/// The titles a person has earned. Whatever rewards a title (a quest, an achievement, a rank) hands it over with GrantAsync.
/// </summary>
public sealed class TitleBook(World world, TitleCatalog catalog, IDbContextFactory<PersistenceContext> factory)
{
    public TitleCatalog Catalog => catalog;

    public bool Owns(Entity player, int title)
    {
        return world.Get<TitleComponent>(player).Owned?.Contains(title) == true;
    }

    /// <summary>
    /// A title can be worn if there is none (zero), or it is one the person has, of the kind of the place it goes to and
    /// of their class.
    /// </summary>
    public bool CanWear(Entity player, int title, TitleKind kind)
    {
        if (title <= 0)
        {
            return true;
        }

        return catalog.TryGet(title, out var definition) &&
               definition.Kind == kind &&
               definition.CanBeUsedBy(world.Get<PersonInfoComponent>(player).Hero) &&
               Owns(player, title);
    }

    /// <summary>
    /// Gives the person a title they can use and do not have yet. They are told, and it is kept.
    /// </summary>
    public async ValueTask<bool> GrantAsync(Entity player, int title, TitleSource source)
    {
        if (!catalog.TryGet(title, out var definition) ||
            !definition.CanBeUsedBy(world.Get<PersonInfoComponent>(player).Hero) ||
            Owns(player, title))
        {
            return false;
        }

        var current = world.Get<TitleComponent>(player);
        var owned = (int[])[.. current.Owned ?? [], title];

        world.Set(player, current with { Owned = owned });
        world.Get<ServerSessionComponent>(player).Send(new PersonAddTitleResponse { Title = title, Value = (int)source });

        await factory.SaveTitleListAsync(world.Get<ActorComponent>(player).Identifier, owned);

        return true;
    }

    /// <summary>
    /// Gives the person every title they can use, and the client the whole list instead of one at a time.
    /// </summary>
    public async ValueTask<int> GrantAllAsync(Entity player)
    {
        var hero = world.Get<PersonInfoComponent>(player).Hero;
        var current = world.Get<TitleComponent>(player);
        var had = current.Owned ?? [];

        var added = catalog.All
            .Where(e => e.CanBeUsedBy(hero) && !had.Contains(e.Id))
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .ToArray();

        if (added.Length == 0)
        {
            return 0;
        }

        int[] owned = [.. had, .. added];

        world.Set(player, current with { Owned = owned });
        Send(player);

        await factory.SaveTitleListAsync(world.Get<ActorComponent>(player).Identifier, owned);

        return added.Length;
    }

    /// <summary>
    /// Tells the client which titles the person has. A list longer than the client can take comes in pieces, each adding
    /// to what it already knows.
    /// </summary>
    public void Send(Entity player)
    {
        var session = world.Get<ServerSessionComponent>(player);

        foreach (var message in Messages(world.Get<TitleComponent>(player).Owned ?? []))
        {
            session.Send(message);
        }
    }

    public static IEnumerable<PersonLoadTitleResponse> Messages(IReadOnlyList<int> owned)
    {
        var limit = PersonLoadTitleResponse.MaxListLength;

        for (var from = 0; from == 0 || from < owned.Count; from += limit)
        {
            yield return new PersonLoadTitleResponse
            {
                TitleList = owned.Skip(from).Take(limit).ToArray(),
                OpenList = [],
                Result = true
            };
        }
    }
}
