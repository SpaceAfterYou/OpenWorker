using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The stats a player was last told: its origin stats (its hero at its level) and its stats with everything on them.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct ShownStatsComponent
{
    public CombatStats Origin { get; init; }

    public CombatStats Final { get; init; }

    /// <summary>
    /// The player has been told its stats on this server.
    /// </summary>
    public bool Known { get; init; }
}

/// <summary>
/// Where the life of a player is kept by something that tells the others (the vitals of a maze, which a party hears): the
/// greatest life is set there, or the player keeps it in its abilities.
/// </summary>
public interface IPlayerHealthResize
{
    /// <summary>
    /// Sets the greatest life of the player; <c>false</c> when the player is not where this keeps its life.
    /// </summary>
    bool Resize(Entity player, int maximum);
}

/// <summary>
/// Tells the player what its stats are, and keeps what it was told in step with what it has: at entry the whole of both lists,
/// after anything that changes them (a level, gear, an upgrade, a gem, a broach, repairs and wear, a title, a buff) the stats
/// that changed. The greatest life and the sizes of the gauges follow, and the fight reads the same stats
/// (<see cref="CombatTables.GetPlayer"/>).
/// </summary>
public sealed class PlayerStatsService(World world, CombatTables combat, PlayerGauges? gauges = null, IPlayerHealthResize? health = null)
{
    /// <summary>The client keeps this many stats; it logs the others and drops them.</summary>
    private const int ClientStats = 61;

    /// <summary>A change of the stats of the player, which the client may write into its chat.</summary>
    private const byte Changed = 0;

    /// <summary>The whole list, sent at entry.</summary>
    private const byte Everything = 1;

    public CombatStats Of(Entity player)
    {
        return combat.GetPlayer(world, player);
    }

    /// <summary>
    /// The stats of the hero of the player at its level, with nothing added: the origin stats of the client.
    /// </summary>
    public CombatStats Origin(Entity player)
    {
        return combat.GetOrigin(world, player);
    }

    /// <summary>
    /// Tells the player all of its stats, as it enters a server: its origin stats (03/51) and its stats with everything on them
    /// (03/34, the whole list).
    /// </summary>
    public void SendAll(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<PersonInfoComponent>(player))
        {
            return;
        }

        var origin = Origin(player);
        var final = Of(player);

        Remember(player, origin, final);
        KeepSpeeds(player, final);

        // The life the player enters with is kept to the greatest its stats give.
        if (world.TryGet<AbilityComponent>(player, out var ability) && ability.Health.Max != (int)Math.Max(1f, final.MaxHealth))
        {
            ResizeHealth(player, final.MaxHealth);
        }

        if (!world.TryGet<ServerSessionComponent>(player, out var session))
        {
            return;
        }

        var actor = world.Get<ActorComponent>(player);

        // The person record went out before the stats were worked out, with the life the person had then; the life and the
        // gauges it has now go with their greatest, or the client keeps the old ones: a soul gauge the client holds at
        // zero leaves every skill unpayable while the server, with a full gauge, sends nothing more. The gauges a maze keeps
        // are sent as they are; a city keeps them full.
        var full = !world.TryGet<GaugeComponent>(player, out var gauges) || !gauges.Ready;

        ST_STAT[] stats =
        [
            .. List(final.Values()),
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_HP_CUR, fValue = world.Get<AbilityComponent>(player).Health.Current },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_SG_CUR, fValue = full ? final.SoulGaugeMax : gauges.SoulGauge },
            new ST_STAT { wStatID = (short)STAT_TYPE.STAT_ST_CUR, fValue = full ? final.StaminaMax : gauges.Stamina }
        ];

        session.Send(new PersonUpdateOriginStatListResponse { Actor = actor, Stats = OriginList(origin) });
        session.Send(new PersonUpdateStatListResponse { Type = Everything, Actor = actor, Stats = stats });
    }

    /// <summary>
    /// Something the stats depend on changed (a booster began or ended): the player is told what changed in its stats if it has
    /// been told them already. A player who is still entering is told everything when it has entered.
    /// </summary>
    public void Refresh(Entity player)
    {
        if (world.IsAlive(player) && world.TryGet<ShownStatsComponent>(player, out var shown) && shown.Known)
        {
            Send(player);
        }
    }

    /// <summary>
    /// Works the stats of the player out again and tells it what changed: its origin stats when its level changed them (03/51),
    /// and the stats that differ from what it was told (03/34). The greatest life and the sizes of the gauges follow the new
    /// stats. Nothing is sent when nothing changed.
    /// </summary>
    public void Send(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<PersonInfoComponent>(player))
        {
            return;
        }

        if (!world.TryGet<ShownStatsComponent>(player, out var shown) || !shown.Known)
        {
            SendAll(player);
            return;
        }

        var origin = Origin(player);
        var final = Of(player);

        Remember(player, origin, final);
        KeepSpeeds(player, final);

        var changed = CombatStats.Differences(shown.Final, final);
        var stats = new List<ST_STAT>(changed.Count + 3);

        foreach (var (type, value) in changed)
        {
            if ((int)type < ClientStats)
            {
                stats.Add(new ST_STAT { wStatID = (short)type, fValue = value });
            }
        }

        if (final.MaxHealth != shown.Final.MaxHealth && ResizeHealth(player, final.MaxHealth) is { } current)
        {
            stats.Add(new ST_STAT { wStatID = (short)STAT_TYPE.STAT_HP_CUR, fValue = current });
        }

        if (gauges is not null && (final.SoulGaugeMax != shown.Final.SoulGaugeMax || final.StaminaMax != shown.Final.StaminaMax))
        {
            stats.AddRange(gauges.Resize(player, final.SoulGaugeMax, final.StaminaMax));
        }

        if (!world.TryGet<ServerSessionComponent>(player, out var session))
        {
            return;
        }

        var actor = world.Get<ActorComponent>(player);

        if (origin != shown.Origin)
        {
            session.Send(new PersonUpdateOriginStatListResponse { Actor = actor, Stats = OriginList(origin) });
        }

        if (stats.Count > 0)
        {
            session.Send(new PersonUpdateStatListResponse { Type = Changed, Actor = actor, Stats = stats });
        }
    }

    /// <summary>
    /// The speeds in the stats are the ones the player moves and attacks with: the move that the pace of the server allows
    /// follows them. A hero with no speed in its stats keeps the speed of the table.
    /// </summary>
    private void KeepSpeeds(Entity player, in CombatStats stats)
    {
        if (stats.MoveSpeed <= 0f || !world.TryGet<AbilityComponent>(player, out var ability))
        {
            return;
        }

        if (ability.Speed.Move != stats.MoveSpeed || ability.Speed.Attack != stats.AttackSpeed)
        {
            world.Set(player, ability with { Speed = new SpeedComponentEntry { Move = stats.MoveSpeed, Attack = stats.AttackSpeed } });
        }
    }

    private void Remember(Entity player, in CombatStats origin, in CombatStats final)
    {
        var shown = new ShownStatsComponent { Origin = origin, Final = final, Known = true };

        if (world.Has<ShownStatsComponent>(player))
        {
            world.Set(player, shown);
        }
        else
        {
            world.Add(player, shown);
        }
    }

    /// <summary>
    /// Sets the greatest life of the player: where something else keeps its life (the vitals of a maze) through it, otherwise in
    /// the abilities of the player, which a city keeps full. Returns the life the player has now when it changed.
    /// </summary>
    private int? ResizeHealth(Entity player, float maximum)
    {
        if (!world.TryGet<AbilityComponent>(player, out var before))
        {
            return null;
        }

        var greatest = (int)Math.Max(1f, maximum);

        if (health is null || !health.Resize(player, greatest))
        {
            world.Set(player, before with { Health = new AbilityComponentEntry { Current = greatest, Max = greatest } });
        }

        var after = world.Get<AbilityComponent>(player).Health.Current;

        return after != before.Health.Current ? after : null;
    }

    private static ST_STAT[] List((STAT_TYPE Type, float Value)[] values)
    {
        var stats = new List<ST_STAT>(values.Length);

        foreach (var (type, value) in values)
        {
            if ((int)type < ClientStats)
            {
                stats.Add(new ST_STAT { wStatID = (short)type, fValue = value });
            }
        }

        return [.. stats];
    }

    private static StatKeyValuePair[] OriginList(in CombatStats origin)
    {
        var values = origin.Values();
        var stats = new List<StatKeyValuePair>(values.Length);

        foreach (var (type, value) in values)
        {
            if ((int)type < ClientStats)
            {
                stats.Add(new StatKeyValuePair { byIndex = type, statValue = value });
            }
        }

        return [.. stats];
    }
}
