using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The soul gauge and the stamina of a player, which skills cost and which fill by themselves.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct GaugeComponent
{
    public float SoulGauge { get; init; }

    public float Stamina { get; init; }

    /// <summary>
    /// The time since the soul gauge last filled, in seconds: it fills once a second.
    /// </summary>
    public float SoulClock { get; init; }

    public float StaminaClock { get; init; }

    /// <summary>
    /// How long the soul gauge waits before it fills again, in seconds, for a hero whose gauge rests after a skill.
    /// </summary>
    public float SoulRest { get; init; }

    /// <summary>
    /// Whether the gauges have been filled for the fight; until then they are not counted.
    /// </summary>
    public bool Ready { get; init; }
}

/// <summary>
/// What a skill costs, by the attribute of its cost in the skill table.
/// </summary>
public enum SkillCostKind : byte
{
    None = 0,
    Health = 1,
    SoulGauge = 2,
    Stamina = 3,
    SoulVapor = 4
}

/// <summary>
/// Fills, spends and reports the soul gauge and the stamina of players. A gauge fills once a second by the regeneration of
/// the hero, and the player is told every change of the current value (03/34).
/// </summary>
public sealed class PlayerGauges(World world, CombatTables tables, ReadOnlyCollection<SoulGaugeRuleRow>? rules = null)
{
    /// <summary>
    /// The rule of a hero whose gauge rests after a skill that is not a plain move.
    /// </summary>
    private const byte RestsAfterSkills = 5;

    private const byte LowestSkippingGroup = 1;

    private const float Second = 1f;

    private readonly RowIndex<byte, SoulGaugeRuleRow> _rules = (rules ?? new ReadOnlyCollection<SoulGaugeRuleRow>([])).IndexBy(e => e.Hero);

    /// <summary>
    /// Fills both gauges, as a player who enters the fight has them, and tells the player their sizes.
    /// </summary>
    public void Fill(Entity player)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var stats = tables.GetPlayer(world, player);

        world.Set(player, new GaugeComponent { SoulGauge = stats.SoulGaugeMax, Stamina = stats.StaminaMax, Ready = true });

        Mirror(player, stats.SoulGaugeMax, stats.SoulGaugeMax, stats.StaminaMax, stats.StaminaMax);

        Send(player,
        [
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_MAX, fValue = stats.SoulGaugeMax },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_REG, fValue = stats.SoulGaugeRegen },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_CUR, fValue = stats.SoulGaugeMax },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_MAX, fValue = stats.StaminaMax },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_REG, fValue = stats.StaminaRegen },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_CUR, fValue = stats.StaminaMax }
        ]);
    }

    public GaugeComponent Of(Entity player)
    {
        return world.TryGet<GaugeComponent>(player, out var gauges) ? gauges : default;
    }

    /// <summary>
    /// Lets time pass for the gauges of a player: every whole second each fills by its regeneration, up to its size.
    /// </summary>
    public void Advance(Entity player, float seconds)
    {
        if (seconds <= 0f || !world.IsAlive(player) || !world.TryGet<GaugeComponent>(player, out var gauges) || !gauges.Ready)
        {
            return;
        }

        var soulClock = gauges.SoulClock + seconds;
        var staminaClock = gauges.StaminaClock + seconds;
        var rest = Math.Max(0f, gauges.SoulRest - seconds);

        // The soul gauge does not count its second while it rests.
        if (rest > 0f)
        {
            soulClock = 0f;
        }

        if (soulClock < Second && staminaClock < Second)
        {
            world.Set(player, gauges with { SoulClock = soulClock, StaminaClock = staminaClock, SoulRest = rest });

            return;
        }

        var stats = tables.GetPlayer(world, player);
        var hero = world.Get<PersonInfoComponent>(player).Hero;
        var soul = gauges.SoulGauge;
        var stamina = gauges.Stamina;
        List<ST_STAT>? changed = null;

        while (soulClock >= Second)
        {
            soulClock -= Second;

            var filled = Math.Min(stats.SoulGaugeMax, soul + SoulGauge.RegenerationPerSecond(hero, stats.Level, stats.SoulGaugeMax, soul, stats.SoulGaugeConstant));

            if (filled != soul)
            {
                soul = filled;
                (changed ??= []).Add(new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_CUR, fValue = soul });
            }
        }

        while (staminaClock >= Second)
        {
            staminaClock -= Second;

            var filled = Math.Min(stats.StaminaMax, stamina + stats.StaminaRegen);

            if (filled != stamina)
            {
                stamina = filled;
                (changed ??= []).Add(new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_CUR, fValue = stamina });
            }
        }

        world.Set(player, gauges with { SoulGauge = soul, Stamina = stamina, SoulClock = soulClock, StaminaClock = staminaClock, SoulRest = rest });

        if (changed is not null)
        {
            Mirror(player, soul, stats.SoulGaugeMax, stamina, stats.StaminaMax);
            Send(player, [.. changed]);
        }
    }

    /// <summary>
    /// Whether the player can pay a cost now. The life must stay above the cost, the gauges must hold it.
    /// </summary>
    public bool CanPay(Entity player, SkillCostKind kind, float cost)
    {
        if (cost <= 0f)
        {
            return true;
        }

        var gauges = Of(player);

        return kind switch
        {
            SkillCostKind.Health => world.TryGet<AbilityComponent>(player, out var ability) && cost < ability.Health.Current,
            SkillCostKind.SoulGauge => !gauges.Ready || cost <= (int)gauges.SoulGauge,
            SkillCostKind.Stamina => !gauges.Ready || cost <= (int)gauges.Stamina,
            SkillCostKind.SoulVapor => world.TryGet<AbilityComponent>(player, out var vapor) && cost <= vapor.SoulVapor.Current,
            _ => true
        };
    }

    /// <summary>
    /// Takes a cost the player can pay, and tells the player.
    /// </summary>
    public void Pay(Entity player, SkillCostKind kind, float cost)
    {
        if (cost <= 0f || !world.TryGet<GaugeComponent>(player, out var gauges) || !gauges.Ready)
        {
            return;
        }

        switch (kind)
        {
            case SkillCostKind.SoulGauge:
                gauges = gauges with { SoulGauge = Math.Max(0f, gauges.SoulGauge - cost) };
                world.Set(player, gauges);
                Send(player, [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_CUR, fValue = gauges.SoulGauge }]);
                break;

            case SkillCostKind.Stamina:
                // A use of stamina starts its second again.
                gauges = gauges with { Stamina = Math.Max(0f, gauges.Stamina - cost), StaminaClock = 0f };
                world.Set(player, gauges);
                Send(player, [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_CUR, fValue = gauges.Stamina }]);
                break;

            default:
                return;
        }

        var ability = world.Get<AbilityComponent>(player);

        Mirror(player, gauges.SoulGauge, ability.SoulGain.Max, gauges.Stamina, ability.Stamina.Max);
    }

    /// <summary>
    /// A skill was used: a hero whose soul gauge rests after a skill waits before it fills again.
    /// </summary>
    public void Rest(Entity player, byte skipPriorityGroup)
    {
        if (skipPriorityGroup <= LowestSkippingGroup || !world.TryGet<GaugeComponent>(player, out var gauges) || !gauges.Ready)
        {
            return;
        }

        var hero = world.Get<PersonInfoComponent>(player).Hero;

        if (_rules.TryGet((byte)hero, out var rule) && rule.Regeneration == RestsAfterSkills)
        {
            world.Set(player, gauges with { SoulRest = rule.Delay / 1000f, SoulClock = 0f });
        }
    }

    /// <summary>
    /// What the hits of one attack gave to the soul gauge of the player, up to its size. The player is told once.
    /// </summary>
    public void Absorb(Entity player, float amount, float maximum)
    {
        if (amount <= 0f || !world.TryGet<GaugeComponent>(player, out var gauges) || !gauges.Ready)
        {
            return;
        }

        var soul = Math.Min(maximum, gauges.SoulGauge + amount);

        if (soul == gauges.SoulGauge)
        {
            return;
        }

        world.Set(player, gauges with { SoulGauge = soul });

        var ability = world.Get<AbilityComponent>(player);

        Mirror(player, soul, maximum, gauges.Stamina, ability.Stamina.Max);
        Send(player, [new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_CUR, fValue = soul }]);
    }

    /// <summary>
    /// Keeps the abilities the character info shows in step with the gauges.
    /// </summary>
    private void Mirror(Entity player, float soul, float soulMax, float stamina, float staminaMax)
    {
        if (!world.TryGet<AbilityComponent>(player, out var ability))
        {
            return;
        }

        world.Set(player, ability with
        {
            SoulGain = new AbilityComponentEntry { Current = (int)soul, Max = (int)soulMax },
            Stamina = new AbilityComponentEntry { Current = (int)stamina, Max = (int)staminaMax }
        });
    }

    private void Send(Entity player, ST_STAT[] stats)
    {
        if (!world.TryGet<ServerSessionComponent>(player, out var session))
        {
            return;
        }

        session.Send(new PersonUpdateStatListResponse
        {
            Actor = world.Get<ActorComponent>(player),
            Stats = stats
        });
    }
}
