using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Skill;

/// <summary>
/// The skills of a person as they are kept: points, what is learned, the decks.
/// </summary>
public sealed class SkillBook
{
    public short TotalPoints { get; set; }
    public short FreePoints { get; set; }
    public List<LearnedSkill> Learned { get; set; } = [];
    public short DeckSlotCount { get; set; }
    public List<SkillDeck> Decks { get; set; } = [];
    public ushort[] DeckBonus { get; set; } = [];
}

public sealed record LearnedSkill(int Skill, int Divergence);

public sealed record SkillDeck(ushort Position, int Skill1, int Skill2, int Skill3, int Skill4);

/// <summary>
/// Loads the skills of a person when it enters a district or a maze, and turns the skill components back into a
/// <see cref="SkillBook"/> for the flush. A person that has never been saved starts with the skills of its class, its active
/// skills in the first deck.
/// </summary>
public sealed class PersonSkills(World world, PersonStateStore store, ReadOnlyCollection<CharacterInfoRow> characters,
    ReadOnlyCollection<SkillRow>? skills = null, SkillLearning? learning = null)
{
    private readonly RowIndex<byte, CharacterInfoRow> _heroes = characters.Where(e => e.Id != 0).IndexBy(e => e.Character);
    private readonly RowIndex<int, SkillRow> _skills = (skills ?? new ReadOnlyCollection<SkillRow>([])).IndexBy(e => e.Id);

    /// <summary>
    /// The points a person starts with when the server does not have its own table of points per level. With the table a
    /// person starts with none and has what its level has given.
    /// </summary>
    public const short StartTotalPoints = 32;

    public const short StartFreePoints = 16;

    /// <summary>
    /// The skills each position of a deck holds at first; one more is bought with an item.
    /// </summary>
    public const short StartDeckSlots = 3;

    private const byte ActiveSkill = 2;

    private static ushort[] StartDeckBonus => [1, 2, 3, 4];

    public async ValueTask LoadAsync(Entity player, int person, Hero hero, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<SkillBook>(person, PersonStateKind.Skills, cancellationToken) ?? CreateNew(hero);

        if (!world.IsAlive(player))
        {
            return;
        }

        world.Set(player, new SkillPointComponent
        {
            TotalSkillPoint = book.TotalPoints,
            FreeSkillPoint = book.FreePoints
        });

        world.Set(player, new SkillLearnedComponent
        {
            Values = book.Learned.Select(e => new SkillInfoValue { Skill = e.Skill, Divergence = e.Divergence }).ToArray()
        });

        world.Set(player, new SkillDeckComponent
        {
            SlotList = book.Decks
                .Select(e => new SkillDeckValue { Position = e.Position, Skill1 = e.Skill1, Skill2 = e.Skill2, Skill3 = e.Skill3, Skill4 = e.Skill4 })
                .ToArray(),
            SlotCount = book.DeckSlotCount
        });

        world.Set(player, new SkillDeckBonusComponent
        {
            Values = book.DeckBonus
        });

        // The level may have given points the book does not have yet.
        learning?.CatchUp(player);
    }

    /// <summary>
    /// Tells the player its skills, points and decks, as it enters a server (06/70).
    /// </summary>
    public void Send(Entity player)
    {
        if (!world.TryGet<ServerSessionComponent>(player, out var session) ||
            !world.TryGet<SkillPointComponent>(player, out var points) ||
            !world.TryGet<SkillLearnedComponent>(player, out var learned) ||
            !world.TryGet<SkillDeckComponent>(player, out var deck) ||
            !world.TryGet<SkillDeckBonusComponent>(player, out var bonus))
        {
            return;
        }

        session.Send(new SkillLoadInfoResponse
        {
            Snapshot = new SkillSnapshotValue
            {
                Target = world.Get<ActorComponent>(player),
                SkillPoint = new SkillPointValue
                {
                    TotalSkillPoint = points.TotalSkillPoint,
                    FreeSkillPoint = points.FreeSkillPoint
                },
                DeckSlotCount = deck.SlotCount,
                DeckBonus = bonus.Values ?? [],
                SkillList = learned.Values ?? [],
                DeckSlotList = deck.SlotList ?? []
            }
        });
    }

    /// <summary>
    /// The skills a class starts with, none of them diverged, each once.
    /// </summary>
    public SkillInfoValue[] DefaultSkills(Hero hero)
    {
        return _heroes.TryGet((byte)hero, out var row)
            ? row.GetSkillList().Where(id => id > 0).Distinct().Select(id => new SkillInfoValue { Skill = id, Divergence = 0 }).ToArray()
            : [];
    }

    private SkillBook CreateNew(Hero hero)
    {
        var learned = DefaultSkills(hero);

        // The active skills of the class go into the first positions of the deck, one in each, so that a new character can
        // use them from the start.
        var decks = learned
            .Where(e => _skills.TryGet(e.Skill, out var row) && row.SkillType == ActiveSkill && row.PassiveType == 0 && row.UsePosition != 2)
            .Take(SkillLearning.DeckPositions)
            .Select((e, position) => new SkillDeck((ushort)position, e.Skill, 0, 0, 0))
            .ToList();

        var known = learning is { KnowsPoints: true };

        return new SkillBook
        {
            TotalPoints = known ? (short)0 : StartTotalPoints,
            FreePoints = known ? (short)0 : StartFreePoints,
            Learned = learned.Select(e => new LearnedSkill(e.Skill, e.Divergence)).ToList(),
            DeckSlotCount = StartDeckSlots,
            Decks = decks,
            DeckBonus = StartDeckBonus
        };
    }

    /// <summary>
    /// The book of the player as it is now, or null when its skills are not loaded.
    /// </summary>
    public static SkillBook? Snapshot(World world, Entity player)
    {
        if (!world.TryGet<SkillLearnedComponent>(player, out var learned) || learned.Values is null ||
            !world.TryGet<SkillPointComponent>(player, out var points) ||
            !world.TryGet<SkillDeckComponent>(player, out var deck) || deck.SlotList is null ||
            !world.TryGet<SkillDeckBonusComponent>(player, out var bonus) || bonus.Values is null)
        {
            return null;
        }

        return new SkillBook
        {
            TotalPoints = points.TotalSkillPoint,
            FreePoints = points.FreeSkillPoint,
            Learned = learned.Values.Where(e => e.Skill != 0).Select(e => new LearnedSkill(e.Skill, e.Divergence)).ToList(),
            DeckSlotCount = deck.SlotCount,
            Decks = deck.SlotList.Select(e => new SkillDeck(e.Position, e.Skill1, e.Skill2, e.Skill3, e.Skill4)).ToList(),
            DeckBonus = (ushort[])bonus.Values.Clone()
        };
    }
}

/// <summary>
/// The skill document of a player for the flush: four components make one book.
/// </summary>
public sealed class SkillDocumentSource : IPersonDocumentSource
{
    public PersonSections Section => PersonSections.Skills;

    public PersonStateKind Kind => PersonStateKind.Skills;

    public string? Snapshot(World world, Entity player)
    {
        return PersonSkills.Snapshot(world, player) is { } book
            ? System.Text.Json.JsonSerializer.Serialize(book, PersonStateStore.Options)
            : null;
    }
}

public static class PersonSkillsServiceCollectionExtensions
{
    public static IServiceCollection AddPersonSkills(this IServiceCollection services)
    {
        services.TryAddSingleton<PersonStateStore>();
        services.AddSingleton<PersonSkills>();
        services.TryAddSingleton<SkillLearning>();
        services.TryAddSingleton<PlayerGauges>();
        services.TryAddSingleton<SkillUse>();
        services.AddSingleton<IPersonDocumentSource, SkillDocumentSource>();

        return services;
    }
}
