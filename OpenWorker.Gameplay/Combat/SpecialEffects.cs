namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The effects of gear, titles and buffs that are not stats: a share of damage to the monsters of a kind, a share off the
/// cooldown of skills, a share of the soul gauge cost, and the like. The tables name them by number, from 100.
/// </summary>
public enum SpecialEffect : byte
{
    /// <summary>
    /// The share in percent by which every skill cools down faster.
    /// </summary>
    CooldownShare = 100,

    /// <summary>
    /// More damage to a plain monster, and to a monster that is a named one, a boss or a raid boss.
    /// </summary>
    DamageToPlain = 103,
    DamageToBoss = 104,

    /// <summary>
    /// More damage to a monster that fights close by, and to one that shoots (none of this client does).
    /// </summary>
    DamageToMelee = 105,
    DamageToRanged = 106,

    /// <summary>
    /// More damage to a monster in the air, and to one on the ground.
    /// </summary>
    DamageToFlying = 107,
    DamageToDown = 108,

    /// <summary>
    /// Less damage from a plain monster and from a boss, and from a monster that fights close by and one that shoots.
    /// </summary>
    DamageFromPlain = 109,
    DamageFromBoss = 110,
    DamageFromMelee = 111,
    DamageFromRanged = 112,

    /// <summary>
    /// More damage to the super armor of a monster.
    /// </summary>
    SuperArmorDamage = 113,

    /// <summary>
    /// A share in percent off the soul gauge cost of a skill.
    /// </summary>
    SoulCostDown = 114,

    /// <summary>
    /// Life and soul gauge a kill gives back.
    /// </summary>
    HealthOnKill = 121,
    SoulGaugeOnKill = 122,

    /// <summary>
    /// Less damage from a blow that missed.
    /// </summary>
    GrazeDamageDown = 142
}

/// <summary>
/// The special effects a character has, by number.
/// </summary>
public sealed class SpecialEffects : IEquatable<SpecialEffects>
{
    public const int First = 100;
    public const int Last = 154;
    public const int Count = Last - First + 1;

    /// <summary>
    /// Nobody has any.
    /// </summary>
    public static SpecialEffects None { get; } = new(new float[Count]);

    private readonly float[] _values;

    public SpecialEffects(float[] values)
    {
        _values = values;
    }

    public static bool IsSpecial(int effect) => effect is >= First and <= Last;

    /// <summary>
    /// The character has none.
    /// </summary>
    public bool IsNone => _values.AsSpan().IndexOfAnyExcept(0f) < 0;

    /// <summary>
    /// Two characters have the same effects when they have the same numbers.
    /// </summary>
    public bool Equals(SpecialEffects? other) => other is not null && _values.AsSpan().SequenceEqual(other._values);

    public override bool Equals(object? obj) => obj is SpecialEffects other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var value in _values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public float this[int effect] => IsSpecial(effect) ? _values[effect - First] : 0f;

    public float Of(SpecialEffect effect) => this[(int)effect];

    /// <summary>
    /// More damage to the monsters of a faction. Factions that no effect is made for have none.
    /// </summary>
    public float DamageToFaction(int faction) => FactionEffect(faction, AddedByFaction);

    /// <summary>
    /// Less damage from the monsters of a faction.
    /// </summary>
    public float DamageFromFaction(int faction) => FactionEffect(faction, TakenByFaction);

    private float FactionEffect(int faction, int[] effects)
    {
        var index = Array.IndexOf(FactionOffsets, faction - FirstFaction);

        return index < 0 ? 0f : this[effects[index]];
    }

    /// <summary>
    /// The faction the first of the effects of a faction is made for; the others come after it, with gaps.
    /// </summary>
    private const int FirstFaction = 6;

    private static readonly int[] FactionOffsets = [0, 1, 2, 3, 10, 11, 12, 13, 22, 23];
    private static readonly int[] AddedByFaction = [126, 127, 128, 129, 130, 131, 132, 133, 151, 153];
    private static readonly int[] TakenByFaction = [134, 135, 136, 137, 138, 139, 140, 141, 152, 154];
}
