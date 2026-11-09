using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// Whether a buff helps or hurts the one who carries it, as the buff table says.
/// </summary>
public enum BuffKind : byte
{
    Buff = 0,
    Debuff = 1,
    Combined = 2
}

/// <summary>
/// What a buff does, by the effect column of the buff table. The ones the server does nothing for are left out.
/// </summary>
public enum BuffEffect : ushort
{
    /// <summary>Changes up to three stats by its values; a buff with a tick gives the current values again at each tick.</summary>
    Stats = 0,
    SuperArmor = 4,
    Invincible = 5,

    /// <summary>With the stat 66 the skills of the carrier cost no soul gauge and cool down by the value, in percent.</summary>
    FreeSkills = 7,

    /// <summary>Only one of these per owner: buffs of this effect from different owners stand side by side.</summary>
    PerOwner = 10,

    /// <summary>Takes away the buffs of the effect named by its first value.</summary>
    Cleanse = 11,
    DoubleSoulAbsorb = 15,

    /// <summary>The cooldowns of the carrier are shorter by its first value, in hundredths of a percent.</summary>
    Cooldown = 17,

    /// <summary>What a skill costs in stamina is less by its first value, in percent.</summary>
    StaminaCost = 18,

    /// <summary>The carrier turns on the one who gave it.</summary>
    Taunt = 21,

    /// <summary>Gives the buffs its three values name instead of itself.</summary>
    Bundle = 24,

    /// <summary>Takes its first value, in percent, of the greatest life of the carrier once, when it is given.</summary>
    LifeCut = 26,

    /// <summary>What a skill costs in soul gauge is less by its first value, in percent.</summary>
    SoulCost = 30,

    Burn = 111,
    Poison = 112,
    Shock = 113,
    Bleed = 114,
    Stun = 121,
    Paralysis = 122,
    Sleep = 123,
    Freeze = 124,
    Charm = 125,
    Confusion = 126,
    Silence = 131,
    Weakness = 132,
    Corrosion = 133
}

/// <summary>
/// What a buff is, from its row of the buff table.
/// </summary>
/// <param name="Group">Buffs of one group replace each other or stack, by the rules of <see cref="BuffRules"/>.</param>
/// <param name="SuccessRate">The chance that a hit gives the buff, out of 10000.</param>
/// <param name="Grade">A buff replaces one of its group only from the same grade up.</param>
/// <param name="Duration">How long it lasts, in seconds; zero for as long as nothing takes it away.</param>
/// <param name="StandBy">How long a hit waits before the buff begins, in seconds.</param>
/// <param name="Tick">How often it acts while it lasts, in seconds; zero for never.</param>
/// <param name="OverlapType">How a second one of the group counts: 0 adds a part of its time, 1 starts the time again.</param>
/// <param name="OverlapCount">How many of the group may stack, 1 for one that is only renewed, 0 for one that is never renewed.</param>
/// <param name="KeepOn">What does not take the buff away: 1 death, 2 leaving the game.</param>
/// <param name="CallOutBuff">The buff that follows when this one runs out.</param>
/// <param name="ChangeBuff">The buff the group turns into when it has stacked to the top.</param>
public readonly record struct BuffDefinition(
    ushort Id,
    byte AllowedPlaces,
    BuffKind Kind,
    BuffEffect Effect,
    ushort Group,
    int SuccessRate,
    byte Grade,
    float Duration,
    float StandBy,
    float Tick,
    byte OverlapType,
    byte OverlapCount,
    byte KeepOn,
    ushort CallOutBuff,
    ushort ChangeBuff,
    BuffStat Stat1,
    BuffStat Stat2,
    BuffStat Stat3)
{
    public const byte KeptOnDeath = 1;

    private const float Millisecond = 0.001f;

    /// <summary>
    /// The places a buff may be given in: bit 1 anywhere, 2 a maze, 4 a district, 8 a battle zone.
    /// </summary>
    public const byte Anywhere = 1;

    public const byte Maze = 2;

    public bool Lasts => Duration <= 0f;

    /// <summary>
    /// Burns, poisons, shocks and bleeds: they hurt at every tick.
    /// </summary>
    public bool Hurts => Effect is >= BuffEffect.Burn and <= BuffEffect.Bleed or BuffEffect.Corrosion;

    /// <summary>
    /// Stuns, paralyses, sleeps, freezes, charms and confusions: one of them replaces another.
    /// </summary>
    public bool Controls => Effect is >= BuffEffect.Stun and <= BuffEffect.Confusion;

    public static BuffDefinition From(in BuffRow row)
    {
        return new BuffDefinition(
            row.Id,
            row.AllowedPlaces,
            (BuffKind)row.Kind,
            (BuffEffect)row.Effect,
            row.Group,
            row.SuccessRate,
            row.Grade,
            Math.Max(0, row.DurationMilliseconds) * Millisecond,
            Math.Max(0, row.StandByMilliseconds) * Millisecond,
            Math.Max(0, row.TickMilliseconds) * Millisecond,
            row.OverlapType,
            row.OverlapCount,
            row.DeleteType,
            row.CallOutBuff,
            row.ChangeBuff,
            new BuffStat(row.Stat1, row.Value1),
            new BuffStat(row.Stat2, row.Value2),
            new BuffStat(row.Stat3, row.Value3));
    }
}

/// <summary>
/// One of the three stats a buff names, by the numbers of the stat effects of items and titles, and its value.
/// </summary>
public readonly record struct BuffStat(byte Type, int Value)
{
    public bool IsSet => Type != 0;
}
