using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Skill.Types;
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
/// Loads the skills of a person when it enters a district, and turns the skill components back into a
/// <see cref="SkillBook"/> for the flush. A person that has never been saved starts with the skills of its class.
/// </summary>
public sealed class PersonSkills(World world, PersonStateStore store, ReadOnlyCollection<CharacterInfoRow> characters)
{
    /// <summary>
    /// The points a person starts with. The client counts them against the level, which the server does not do yet.
    /// </summary>
    public const short StartTotalPoints = 32;

    public const short StartFreePoints = 16;

    public const short StartDeckSlots = 4;

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
    }

    /// <summary>
    /// The skills a class starts with, none of them diverged.
    /// </summary>
    public SkillInfoValue[] DefaultSkills(Hero hero)
    {
        foreach (var row in characters)
        {
            if (row.Character == (byte)hero && row.Id != 0)
            {
                return row.GetSkillList().Where(id => id > 0).Select(id => new SkillInfoValue { Skill = id, Divergence = 0 }).ToArray();
            }
        }

        return [];
    }

    private SkillBook CreateNew(Hero hero)
    {
        return new SkillBook
        {
            TotalPoints = StartTotalPoints,
            FreePoints = StartFreePoints,
            Learned = DefaultSkills(hero).Select(e => new LearnedSkill(e.Skill, e.Divergence)).ToList(),
            DeckSlotCount = StartDeckSlots,
            DeckBonus = StartDeckBonus
        };
    }

    /// <summary>
    /// The book of the player as it is now, or null when its skills are not loaded (a maze does not load them).
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
        services.AddSingleton<IPersonDocumentSource, SkillDocumentSource>();

        return services;
    }
}
