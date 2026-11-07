using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Types;

namespace OpenWorker.Gameplay.Modules.Boosters;

public static class BoosterRules
{
    /// <summary>
    /// The seconds a booster has left, as the client counts them.
    /// </summary>
    public static long GetRemainSeconds(BoosterDefinition definition, BoosterEntry entry, long nowUnix)
    {
        return definition.Time switch
        {
            BoosterTime.Endless => BoosterLoadEntry.Endless,
            BoosterTime.Until => Math.Max(0, entry.Remain - nowUnix),
            _ => Math.Max(0, entry.Remain / 1000)
        };
    }

    /// <summary>
    /// Whether the booster has run out.
    /// </summary>
    public static bool IsOver(BoosterDefinition definition, BoosterEntry entry, long nowUnix)
    {
        return definition.Time switch
        {
            BoosterTime.Endless => false,
            BoosterTime.Until => nowUnix >= entry.Remain,
            _ => entry.Remain <= 0
        };
    }

    /// <summary>
    /// Whether time spent in the area counts for the booster.
    /// </summary>
    public static bool Consumes(BoosterTime time, BoosterConsumeArea area)
    {
        return time switch
        {
            BoosterTime.District => area == BoosterConsumeArea.District,
            BoosterTime.Maze => area == BoosterConsumeArea.Maze,
            BoosterTime.Online => true,
            _ => false
        };
    }

    /// <summary>
    /// Takes the time spent from the booster. Returns whether anything was taken.
    /// </summary>
    public static bool Consume(BoosterDefinition definition, BoosterEntry entry, BoosterConsumeArea area, long milliseconds)
    {
        if (!Consumes(definition.Time, area) || milliseconds <= 0)
        {
            return false;
        }

        entry.Remain -= milliseconds;

        return true;
    }

    /// <summary>
    /// Gives a booster the time it is worth, or adds that time to the one it has.
    /// </summary>
    public static void Charge(BoosterDefinition definition, BoosterEntry entry, long durationMilliseconds, long nowUnix)
    {
        switch (definition.Time)
        {
            case BoosterTime.Endless:
                break;

            // The time of a booster that is still running goes on from its end.
            case BoosterTime.Until:
                entry.Remain = Math.Max(entry.Remain, nowUnix) + durationMilliseconds / 1000;
                break;

            default:
                entry.Remain += durationMilliseconds;
                break;
        }
    }

    /// <summary>
    /// The sum of the effects of the boosters that are applied as a part of the amount.
    /// </summary>
    public static float GetRate(IEnumerable<BoosterDefinition> boosters, BoosterEffect effect) =>
        boosters.SelectMany(e => e.Effects).Where(e => e.Effect == effect && e.Apply == BoosterApply.Rate).Sum(e => e.Value);

    /// <summary>
    /// The sum of the effects of the boosters that are applied as an amount.
    /// </summary>
    public static float GetValue(IEnumerable<BoosterDefinition> boosters, BoosterEffect effect) =>
        boosters.SelectMany(e => e.Effects).Where(e => e.Effect == effect && e.Apply == BoosterApply.Value).Sum(e => e.Value);

    /// <summary>
    /// An amount with the boosters applied: the amount, a part of it for the rates, and the values.
    /// </summary>
    public static float Apply(IReadOnlyCollection<BoosterDefinition> boosters, BoosterEffect effect, float origin) =>
        origin + origin * GetRate(boosters, effect) + GetValue(boosters, effect);
}
