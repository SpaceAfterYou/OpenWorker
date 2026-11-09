using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// How fast the soul gauge of a hero fills by itself, a second at a time. Each weapon has a rule of its own; the constant is
/// the soul gauge regeneration of the status of the hero.
/// </summary>
public static class SoulGauge
{
    /// <summary>
    /// What one second adds to the soul gauge.
    /// </summary>
    public static float RegenerationPerSecond(Hero hero, int level, float maximum, float current, float constant)
    {
        return hero switch
        {
            // The gun fills in proportion to the size of its small gauge.
            Hero.Erwin => maximum / 400f * constant,

            Hero.Lily => constant,

            // The arms of the spirit fill fast, and faster with the level.
            Hero.Jin => constant * (maximum * 0.05f) + 10f + level * 0.2f,

            // The guitar fills the faster, the emptier it is.
            Hero.Stella => (maximum / 500f + (maximum - current) / 150f) * constant,

            // The sword, and every hero added after the first five.
            _ => ((int)maximum + 100f) / 200f * constant
        };
    }

    /// <summary>
    /// What one hit of a skill gives to the soul gauge of the attacker: the absorption of the skill times the share the
    /// target lets go of. A gauge that is full takes nothing.
    /// </summary>
    public static float Absorbed(float skillAbsorption, float targetRate, float current, float maximum)
    {
        if (skillAbsorption <= 0f || targetRate <= 0f || current >= maximum)
        {
            return 0f;
        }

        return skillAbsorption * targetRate;
    }
}
