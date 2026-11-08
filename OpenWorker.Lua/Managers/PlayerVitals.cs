using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Lua.Managers;

/// <summary>
/// How much life a player in a maze has left, and whether they can be hurt.
/// </summary>
public sealed class PlayerVitals(World ecs, CombatTables tables)
{
    /// <summary>
    /// The size of a player as a monster hits it: a standing cylinder.
    /// </summary>
    public const float BodyRadius = 35f;

    public const float BodyHeight = 170f;

    /// <summary>
    /// Somebody who has just come back cannot be hurt for this long.
    /// </summary>
    public static readonly TimeSpan ReviveProtection = TimeSpan.FromSeconds(5);

    private Dictionary<Entity, long> Protected { get; } = [];

    public CombatStats StatsOf(Entity player)
    {
        return tables.GetPlayer(ecs, player);
    }

    public int Health(Entity player) => ecs.Get<AbilityComponent>(player).Health.Current;

    public int MaxHealth(Entity player) => ecs.Get<AbilityComponent>(player).Health.Max;

    public bool IsAlive(Entity player) => ecs.IsAlive(player) && Health(player) > 0;

    /// <summary>
    /// Sets the life of a player who has entered a maze to what the class and the level give.
    /// </summary>
    public void Initialize(Entity player)
    {
        var maximum = (int)Math.Max(1f, StatsOf(player).MaxHealth);

        SetHealth(player, maximum, maximum);
    }

    /// <summary>
    /// Whether the player cannot be hurt now.
    /// </summary>
    public bool IsProtected(Entity player)
    {
        return Protected.TryGetValue(player, out var until) && Environment.TickCount64 < until;
    }

    public void Protect(Entity player)
    {
        Protected[player] = Environment.TickCount64 + (long)ReviveProtection.TotalMilliseconds;
    }

    public void Forget(Entity player)
    {
        Protected.Remove(player);
    }

    /// <summary>
    /// Takes life away. Returns what is left.
    /// </summary>
    public int Hurt(Entity player, int amount)
    {
        var ability = ecs.Get<AbilityComponent>(player);
        var left = Math.Max(0, ability.Health.Current - Math.Max(0, amount));

        SetHealth(player, left, ability.Health.Max);

        return left;
    }

    /// <summary>
    /// Fills the life of a player up.
    /// </summary>
    public void Restore(Entity player)
    {
        SetHealth(player, MaxHealth(player), MaxHealth(player));
    }

    public Vector3 PositionOf(Entity player) => ecs.Get<WorldComponent>(player).Position;

    private void SetHealth(Entity player, int current, int maximum)
    {
        var ability = ecs.Get<AbilityComponent>(player);

        ecs.Set(player, ability with { Health = new AbilityComponentEntry { Current = current, Max = maximum } });
    }
}
