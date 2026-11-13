using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The deck bonus of the skill the player started last: its kind and value, none when the skill was not linked. It holds until
/// the next skill starts, for the cost, the cooldown and every blow of that skill.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct SkillDeckLinkComponent(DeckBonusKind Kind, float Value);

/// <summary>
/// A skill started linked from the one before it in a row of the deck: the bonus of its place, which the player chose for the
/// page. The first place of a row links to nothing. The client 1.4.2.3 uses the first page only.
/// </summary>
public static class DeckLink
{
    /// <summary>
    /// The rows of a deck page.
    /// </summary>
    public const int Rows = 6;

    /// <summary>
    /// The places of a row.
    /// </summary>
    public const int Places = 4;

    /// <summary>
    /// The bonus chosen for the place the skill, or its swap skill, holds in the row; zero at the first place, outside the row
    /// and past the rows of the page.
    /// </summary>
    public static ushort BonusOf(SkillDeckValue[]? decks, ushort[]? bonuses, byte row, int skill, int swap)
    {
        if (row >= Rows || decks is null || bonuses is null || skill == 0)
        {
            return 0;
        }

        foreach (var deck in decks)
        {
            if (deck.Position != row)
            {
                continue;
            }

            var place = 0;

            foreach (var standing in deck.GetList())
            {
                if (standing != 0 && (standing == skill || standing == swap))
                {
                    return place > 0 && place - 1 < bonuses.Length ? bonuses[place - 1] : (ushort)0;
                }

                place++;
            }
        }

        return 0;
    }

    /// <summary>
    /// The share of damage the skill the player started last adds to each of its blows.
    /// </summary>
    public static float DamageShare(World world, Entity player)
    {
        return world.TryGet<SkillDeckLinkComponent>(player, out var link) && link is { Kind: DeckBonusKind.Damage, Value: > 0f } ? link.Value : 0f;
    }
}
