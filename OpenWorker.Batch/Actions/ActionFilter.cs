namespace OpenWorker.Batch.Actions;

/// <summary>
/// The modes an event of an animation is for, as the event names them, or the mode the one who plays the animation is in.
/// The first number holds the levels of the skill (the low half, a bit a level), the combat types (the third byte) and the
/// steps of a charge (the high byte); the second the divergences (the low half for the first group, bits from the seventeenth
/// for the second, a level a bit) and the bit of no divergence at all; the third the conditions of the action and of a fight
/// between players. An event with no bits is for every mode.
/// </summary>
public readonly record struct ActionFilter(int First, int Second, int Third)
{
    /// <summary>
    /// The bit of an event that is only for a skill without a divergence.
    /// </summary>
    private const int NoDivergence = 1 << 30;

    private const int AllLevels = 0xFFFF;
    private const int AllCharges = unchecked((int)(0xFFu << 24));
    private const int AllCombatTypes = 0xFF << 16;
    private const int AllConditions = 0xFF;
    private const int AllPlayerFights = 0xFF00;

    /// <summary>
    /// The divergences of the second group are told as a hundred more than the ones of the first.
    /// </summary>
    public const int FirstGroup = 100;

    public const int SecondGroup = 200;

    /// <summary>
    /// Any value of a mode: a mode the one who plays does not know matches every event.
    /// </summary>
    public const int Any = -1;

    public bool IsEmpty => First == 0 && Second == 0 && Third == 0;

    /// <summary>
    /// The mode of the one who plays an animation.
    /// </summary>
    /// <param name="level">The level of the skill, from 1.</param>
    /// <param name="charge">The step the charge of the skill has reached, from 0.</param>
    /// <param name="combatType">The combat type, from 0.</param>
    /// <param name="divergence">Zero for none, <see cref="FirstGroup"/> or <see cref="SecondGroup"/> plus its level otherwise.</param>
    /// <param name="condition">The condition of the action, zero for none.</param>
    /// <param name="playerFight">The kind of fight between players, zero for none.</param>
    public static ActionFilter Of(int level, int charge, int combatType, int divergence, int condition = 0, int playerFight = 0)
    {
        var first = level switch
        {
            Any => AllLevels,
            > 0 and <= 16 => 1 << (level - 1),
            _ => 0
        };

        first |= charge switch
        {
            Any => AllCharges,
            >= 0 and < 8 => 1 << (charge + 24),
            _ => 0
        };

        first |= combatType switch
        {
            Any => AllCombatTypes,
            >= 0 and < 8 => 1 << (combatType + 16),
            _ => 0
        };

        var second = divergence switch
        {
            Any => AllLevels,
            >= SecondGroup and < SecondGroup + 15 => 1 << (divergence % 100 + 15),
            > 0 and < SecondGroup when divergence % 100 is > 0 and <= 16 => 1 << (divergence % 100 - 1),
            _ => 0
        };

        var third = condition switch
        {
            Any => AllConditions,
            > 0 => condition & AllConditions,
            _ => 0
        };

        third |= playerFight switch
        {
            Any => AllPlayerFights,
            > 0 and < 9 => 1 << (playerFight + 7),
            _ => 0
        };

        return new ActionFilter(first, second, third);
    }

    /// <summary>
    /// Whether an event with the given filter is left out for one in this mode.
    /// </summary>
    public bool Excludes(ActionFilter filter)
    {
        if (filter.IsEmpty || IsEmpty)
        {
            return false;
        }

        if (Misses(First & AllLevels, filter.First & AllLevels, AllLevels) ||
            Misses(First & AllCharges, filter.First & AllCharges, AllCharges, mineMayBeEmpty: true) ||
            Misses(First & AllCombatTypes, filter.First & AllCombatTypes, AllCombatTypes, mineMayBeEmpty: true))
        {
            return true;
        }

        if (filter.Second != 0 && (Second & AllLevels) != AllLevels)
        {
            if ((filter.Second & NoDivergence) != 0 && Second != 0)
            {
                return true;
            }

            if ((filter.Second & AllLevels) != 0 && (Second & filter.Second & AllLevels) == 0)
            {
                return true;
            }

            const int groups = 0x3FFF << 16;

            if ((filter.Second & groups) != 0 && (Second & filter.Second & groups) == 0)
            {
                return true;
            }
        }

        if ((filter.Third & AllConditions) != 0 && (filter.Third & AllConditions) != AllConditions)
        {
            return (Third & filter.Third & AllConditions) == 0;
        }

        return Misses(Third & AllPlayerFights, filter.Third & AllPlayerFights, AllPlayerFights, mineMayBeEmpty: false);
    }

    /// <summary>
    /// A part of the filter of an event names some values, and the mode has none of them. A part of the mode that is empty
    /// matches everything when <paramref name="mineMayBeEmpty"/>.
    /// </summary>
    private static bool Misses(int mine, int wanted, int all, bool mineMayBeEmpty = false)
    {
        if (wanted == 0 || wanted == all || (mineMayBeEmpty && mine == 0))
        {
            return false;
        }

        return (mine & wanted) == 0;
    }
}
