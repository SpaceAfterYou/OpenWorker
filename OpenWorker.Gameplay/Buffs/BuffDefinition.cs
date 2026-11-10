using OpenWorker.Gameplay.Combat;
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

    /// <summary>The carrier is not shown or is shown differently; the server only keeps it.</summary>
    Hidden = 2,

    /// <summary>The damage the carrier takes is also taken from its life again, by the value in percent.</summary>
    Reflection = 8,

    /// <summary>The carrier takes back life from the damage it deals, by the value in percent, for as many targets of a hit as its first stat says.</summary>
    Blood = 9,

    /// <summary>Only one of these per owner: buffs of this effect from different owners stand side by side.</summary>
    PerOwner = 10,

    /// <summary>Takes away the buffs of the effect named by its first value.</summary>
    Cleanse = 11,

    /// <summary>The one who gave it, and its friends, take back life from the damage they deal to the carrier.</summary>
    BloodDebuff = 12,

    /// <summary>The damage rolls of the carrier are always the greatest.</summary>
    FixedMaxDamage = 13,

    /// <summary>The damage the summons of the carrier deal once is the value, in percent, of what it would be.</summary>
    MultipleDamageOnce = 14,

    DoubleSoulAbsorb = 15,

    /// <summary>The super armor the attacks of the carrier take from a target is more by the value.</summary>
    BuffSuperArmor = 16,

    /// <summary>The gold the carrier gets is more by the value, in percent.</summary>
    GoldRate = 19,

    /// <summary>The cooldowns of the carrier are shorter by its first value, in hundredths of a percent.</summary>
    Cooldown = 17,

    /// <summary>What a skill costs in stamina is less by its first value, in percent.</summary>
    StaminaCost = 18,

    /// <summary>The carrier turns on the one who gave it.</summary>
    Taunt = 21,

    /// <summary>Gives the buffs its three values name instead of itself.</summary>
    Bundle = 24,

    /// <summary>A hit of the carrier gives its soul gauge a share of the size of the gauge, by the value in ten-thousandths.</summary>
    SoulAbsorb = 25,

    /// <summary>Takes its first value, in percent, of the greatest life of the carrier once, when it is given.</summary>
    LifeCut = 26,

    /// <summary>The damage the carrier does to a boss is more by the value, in percent.</summary>
    BossAttackAdd = 28,

    /// <summary>The damage a boss does to the carrier is less by the value, in percent.</summary>
    BossAttackedDown = 29,

    /// <summary>What a skill costs in soul gauge is less by its first value, in percent.</summary>
    SoulCost = 30,

    /// <summary>All the damage the carrier does is more by the value, in percent.</summary>
    AllAttackAdd = 31,

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
/// <param name="SystemType">0 an ordinary buff; 1 one that a boss is not given; 2 and 3 are not held back by resistances and do not roll their chance, 3 also gets through immunity. Only the server's own table has it.</param>
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
    BuffStat Stat3,
    byte SystemType = 0)
{
    public const byte KeptOnDeath = 1;

    private const float Millisecond = 0.001f;

    /// <summary>
    /// The places a buff may be given in: bit 1 anywhere, 2 a maze, 4 a district, 8 a battle zone.
    /// </summary>
    public const byte Anywhere = 1;

    public const byte Maze = 2;

    public const byte District = 4;

    public const byte BattleZone = 8;

    /// <summary>
    /// Whether the buff may be on someone in a place: the places the table names, or anywhere.
    /// </summary>
    /// <param name="place">One of <see cref="Maze"/>, <see cref="District"/> and <see cref="BattleZone"/>.</param>
    public bool AllowedIn(byte place) => (AllowedPlaces & (Anywhere | place)) != 0;

    /// <summary>
    /// Resistances do not hold the buff back and its chance is not rolled.
    /// </summary>
    public bool IgnoresResistance => SystemType is 2 or 3;

    public bool Lasts => Duration <= 0f;

    /// <summary>
    /// Burns, poisons, shocks and bleeds: they hurt at every tick.
    /// </summary>
    public bool Hurts => Effect is >= BuffEffect.Burn and <= BuffEffect.Bleed or BuffEffect.Corrosion;

    /// <summary>
    /// Stuns, paralyses, sleeps, freezes, charms and confusions: one of them replaces another.
    /// </summary>
    public bool Controls => Effect is >= BuffEffect.Stun and <= BuffEffect.Confusion;

    /// <summary>
    /// What the carrier resists of this buff: the states a buff puts on someone are held back by a resistance.
    /// </summary>
    public float ResistanceOf(in Resistances resist) => Effect switch
    {
        BuffEffect.Burn => resist.Burn,
        BuffEffect.Poison => resist.Poison,
        BuffEffect.Shock => resist.Shock,
        BuffEffect.Bleed => resist.Bleed,
        BuffEffect.Stun => resist.Stun,
        BuffEffect.Paralysis => resist.Paralysis,
        BuffEffect.Sleep => resist.Sleep,
        BuffEffect.Freeze => resist.Freeze,
        BuffEffect.Charm => resist.Charm,
        BuffEffect.Confusion => resist.Confusion,
        BuffEffect.Silence => resist.Silence,
        BuffEffect.Weakness => resist.Weakness,
        _ => 0f
    };

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
