using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Gameplay.Buffs;

/// <summary>
/// What a buff on someone has done so far and how long it has left.
/// </summary>
public sealed class ActiveBuff
{
    internal ActiveBuff(in BuffDefinition definition, int owner)
    {
        Reset(definition, owner);
    }

    public BuffDefinition Definition { get; private set; }

    public ushort Buff => Definition.Id;

    /// <summary>
    /// The actor that gave it.
    /// </summary>
    public int Owner { get; internal set; }

    /// <summary>
    /// Seconds it has left; a buff that lasts has none to count.
    /// </summary>
    public float Remaining { get; internal set; }

    /// <summary>
    /// How many of the group are stacked in it.
    /// </summary>
    public byte Count { get; internal set; }

    /// <summary>
    /// Whether the stats the buff names go into the stats of its carrier. A buff that only shows (the buff of a broach set, whose
    /// stats the worn clothing counts) leaves them out.
    /// </summary>
    public bool StatsCounted { get; internal set; } = true;

    /// <summary>
    /// Seconds since its last tick.
    /// </summary>
    internal float TickClock { get; set; }

    /// <summary>
    /// The values of its three stats, which grow with every stack of a buff that stacks its values.
    /// </summary>
    internal float[] Values { get; } = new float[3];

    internal void Reset(in BuffDefinition definition, int owner)
    {
        Definition = definition;
        Owner = owner;
        Remaining = definition.Duration;
        Count = 1;
        TickClock = 0f;
        Values[0] = definition.Stat1.Value;
        Values[1] = definition.Stat2.Value;
        Values[2] = definition.Stat3.Value;
    }

    internal BuffStat Stat(int index) => index switch
    {
        0 => Definition.Stat1,
        1 => Definition.Stat2,
        _ => Definition.Stat3
    };
}

/// <summary>
/// The buffs that wait for their stand-by time before they begin.
/// </summary>
internal sealed record PendingBuff(BuffDefinition Definition, int Owner)
{
    public float Wait { get; set; } = Definition.StandBy;
}

/// <summary>
/// The states a buff puts its carrier in.
/// </summary>
[Flags]
public enum BuffControl : byte
{
    None = 0,
    Stunned = 1 << 0,
    Paralysed = 1 << 1,
    Asleep = 1 << 2,
    Frozen = 1 << 3,
    Charmed = 1 << 4,
    Confused = 1 << 5,
    Silenced = 1 << 6,
    Weakened = 1 << 7,

    /// <summary>
    /// Neither moves nor attacks.
    /// </summary>
    Held = Stunned | Paralysed | Asleep | Frozen
}

/// <summary>
/// What the buffs of a carrier do to the damage it deals and takes, added up. A rate is a share (0.1 is ten percent).
/// </summary>
public readonly record struct BuffRates
{
    /// <summary>The share of the damage it takes that the carrier loses again.</summary>
    public float Reflect { get; init; }

    /// <summary>The share of the damage it deals that the carrier takes back as life.</summary>
    public float Blood { get; init; }

    /// <summary>How many targets of one hit give the carrier its life back (zero for all of them).</summary>
    public int BloodTargets { get; init; }

    /// <summary>The share of the damage it takes that the one who gave the buff, and its friends, take back as life.</summary>
    public float BloodDebuff { get; init; }

    /// <summary>The actor that gave the buff of <see cref="BloodDebuff"/>.</summary>
    public int BloodDebuffOwner { get; init; }

    /// <summary>The damage rolls of the carrier are always the greatest.</summary>
    public bool FixedMaxDamage { get; init; }

    /// <summary>What the damage of a summon of the carrier is multiplied by, once; zero for no such buff.</summary>
    public float MultipleDamageOnce { get; init; }

    /// <summary>The share of the size of its soul gauge that a hit of the carrier gives it.</summary>
    public float SoulAbsorb { get; init; }

    /// <summary>How many targets of one hit give the carrier its soul gauge (zero for all of them).</summary>
    public int SoulAbsorbTargets { get; init; }

    public float BossAttackAdd { get; init; }

    public float BossAttackedDown { get; init; }

    public float AllAttackAdd { get; init; }

    /// <summary>More of the super armor of a target taken by the attacks of the carrier.</summary>
    public float SuperArmorRate { get; init; }

    /// <summary>More gold for the carrier.</summary>
    public float GoldRate { get; init; }

    /// <summary>The actor the carrier is turned on (a taunt), zero for none.</summary>
    public int Taunt { get; init; }
}

/// <summary>
/// The buffs on a player or a monster and what they add up to. It belongs to the one who carries it, for the time it is in a
/// maze; <see cref="BuffRules"/> changes it.
/// </summary>
public sealed class BuffBook
{
    /// <summary>
    /// The buffs one carrier may have at once.
    /// </summary>
    public const int Capacity = 50;

    private const byte FreeSkillsStat = 66;
    private const float Percent = 0.01f;

    private readonly List<StatEffect> _effects = [];

    internal List<ActiveBuff> Active { get; } = [];

    internal List<PendingBuff> Pending { get; } = [];

    public IReadOnlyList<ActiveBuff> Buffs => Active;

    public bool IsEmpty => Active.Count == 0 && Pending.Count == 0;

    /// <summary>
    /// The stats the buffs change, as the stat effects of items and titles name them.
    /// </summary>
    public ReadOnlySpan<StatEffect> StatEffects => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_effects);

    public BuffControl Control { get; private set; }

    /// <summary>
    /// Neither moves nor attacks: stunned, paralysed, asleep or frozen.
    /// </summary>
    public bool IsHeld => (Control & BuffControl.Held) != 0;

    /// <summary>
    /// How a buff makes the carrier take hits, when one does.
    /// </summary>
    public DefenseType? Defense { get; private set; }

    /// <summary>
    /// How much shorter the cooldowns of the carrier are, in percent.
    /// </summary>
    public float CooldownRate { get; private set; }

    /// <summary>
    /// The skills of the carrier take no soul gauge.
    /// </summary>
    public bool FreeSoulGauge { get; private set; }

    /// <summary>
    /// The share of a stamina cost the carrier does not pay.
    /// </summary>
    public float StaminaCostDown { get; private set; }

    /// <summary>
    /// The share of a soul gauge cost the carrier does not pay.
    /// </summary>
    public float SoulCostDown { get; private set; }

    /// <summary>
    /// A hit of the carrier gives its soul gauge twice as much.
    /// </summary>
    public bool DoubleSoulAbsorb { get; private set; }

    /// <summary>
    /// What the buffs do to the damage dealt and taken.
    /// </summary>
    public BuffRates Rates { get; private set; }

    /// <summary>
    /// Changes with every change of what the buffs add up to.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// The stats with the buffs on them.
    /// </summary>
    public CombatStats Apply(in CombatStats stats)
    {
        return _effects.Count == 0 ? stats : Combat.StatEffects.Apply(stats, StatEffects);
    }

    /// <summary>
    /// Where a buff is in the list of the active ones, -1 when the carrier does not have it.
    /// </summary>
    public int IndexOf(ushort buff) => Find(buff);

    internal int Find(ushort buff)
    {
        for (var index = 0; index < Active.Count; index++)
        {
            if (Active[index].Buff == buff)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// The buff of a group the new one meets: of any owner, or of the same owner for the buffs that stand side by side.
    /// </summary>
    internal int FindGroup(ushort group, int owner)
    {
        for (var index = 0; index < Active.Count; index++)
        {
            var active = Active[index];

            if (active.Definition.Group == group && (active.Definition.Effect != BuffEffect.PerOwner || active.Owner == owner))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Adds up what the buffs do, after any of them came, changed or went.
    /// </summary>
    internal void Recount()
    {
        _effects.Clear();

        var control = BuffControl.None;
        DefenseType? defense = null;
        var cooldown = 0f;
        var freeSoul = false;
        var stamina = 0f;
        var soul = 0f;
        var absorb = false;
        var rates = new BuffRates();

        foreach (var buff in Active)
        {
            var definition = buff.Definition;

            switch (definition.Effect)
            {
                case BuffEffect.Stats or BuffEffect.Weakness when !buff.StatsCounted:
                    break;

                case BuffEffect.Stats:
                case BuffEffect.Weakness:
                    for (var index = 0; index < 3; index++)
                    {
                        var stat = buff.Stat(index);

                        if (stat.IsSet && !BuffRules.IsCurrentValue(stat.Type))
                        {
                            _effects.Add(new StatEffect(stat.Type, buff.Values[index]));
                        }
                    }

                    break;

                case BuffEffect.SuperArmor:
                    defense ??= DefenseType.SuperArmor;
                    break;

                case BuffEffect.Invincible:
                    defense = DefenseType.Invincible;
                    break;

                case BuffEffect.FreeSkills when definition.Stat1.Type == FreeSkillsStat:
                    cooldown = buff.Values[0];
                    freeSoul = true;
                    break;

                case BuffEffect.Cooldown:
                    cooldown += buff.Values[0] * Percent;
                    break;

                case BuffEffect.StaminaCost:
                    stamina = buff.Values[0] * Percent;
                    break;

                case BuffEffect.SoulCost:
                    soul += buff.Values[0] * Percent;
                    break;

                case BuffEffect.DoubleSoulAbsorb:
                    absorb = true;
                    break;

                case BuffEffect.Reflection:
                    rates = rates with { Reflect = Math.Max(rates.Reflect, buff.Values[0] * Percent) };
                    break;

                case BuffEffect.Blood:
                    rates = rates with { Blood = Math.Max(rates.Blood, buff.Values[0] * Percent), BloodTargets = definition.Stat1.Type };
                    break;

                case BuffEffect.BloodDebuff:
                    rates = rates with { BloodDebuff = Math.Max(rates.BloodDebuff, buff.Values[0] * Percent), BloodDebuffOwner = buff.Owner };
                    break;

                case BuffEffect.FixedMaxDamage:
                    rates = rates with { FixedMaxDamage = true };
                    break;

                case BuffEffect.MultipleDamageOnce:
                    rates = rates with { MultipleDamageOnce = buff.Values[0] * Percent };
                    break;

                case BuffEffect.SoulAbsorb:
                    rates = rates with { SoulAbsorb = Math.Max(rates.SoulAbsorb, buff.Values[0] * 0.0001f), SoulAbsorbTargets = definition.Stat1.Type };
                    break;

                case BuffEffect.BossAttackAdd:
                    rates = rates with { BossAttackAdd = rates.BossAttackAdd + buff.Values[0] * Percent };
                    break;

                case BuffEffect.BossAttackedDown:
                    rates = rates with { BossAttackedDown = rates.BossAttackedDown + buff.Values[0] * Percent };
                    break;

                case BuffEffect.AllAttackAdd:
                    rates = rates with { AllAttackAdd = rates.AllAttackAdd + buff.Values[0] * Percent };
                    break;

                case BuffEffect.BuffSuperArmor:
                    rates = rates with { SuperArmorRate = buff.Values[0] * Percent };
                    break;

                case BuffEffect.GoldRate:
                    rates = rates with { GoldRate = buff.Values[0] * Percent };
                    break;

                case BuffEffect.Taunt:
                    rates = rates with { Taunt = buff.Owner };
                    break;
            }

            control |= definition.Effect switch
            {
                BuffEffect.Stun => BuffControl.Stunned,
                BuffEffect.Paralysis => BuffControl.Paralysed,
                BuffEffect.Sleep => BuffControl.Asleep,
                BuffEffect.Freeze => BuffControl.Frozen,
                BuffEffect.Charm => BuffControl.Charmed,
                BuffEffect.Confusion => BuffControl.Confused,
                BuffEffect.Silence => BuffControl.Silenced,
                BuffEffect.Weakness => BuffControl.Weakened,
                _ => BuffControl.None
            };
        }

        Control = control;
        Defense = defense;
        CooldownRate = cooldown;
        FreeSoulGauge = freeSoul;
        StaminaCostDown = stamina;
        SoulCostDown = soul;
        DoubleSoulAbsorb = absorb;
        Rates = rates;
        Version++;
    }

    /// <summary>
    /// The book of a player, or null when it has none (it is not in a maze).
    /// </summary>
    public static BuffBook? Of(World world, Entity entity)
    {
        return world.TryGet<BuffBookComponent>(entity, out var component) ? component.Book : null;
    }
}

/// <summary>
/// The buffs a player carries in a maze. The maze gives it when the player comes in and takes it when the player leaves.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct BuffBookComponent
{
    public BuffBook? Book { get; init; }
}
