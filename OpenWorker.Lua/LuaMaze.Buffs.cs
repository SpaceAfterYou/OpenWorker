using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Buffs;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Lua.Buffs;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

/// <summary>
/// The buffs of a maze: who carries them, what a hit and a script give, what a buff that kills does.
/// </summary>
public partial class LuaMaze
{
    private static MazeBuffs CreateBuffs(World ecs, LuaCreatureManager creatures, BuffManager manager, CombatTables combat, Entity first)
    {
        var buffs = new MazeBuffs(ecs, creatures, manager.Catalog, combat, creatures.Services?.Gauges, manager);

        buffs.Attach(first);

        return buffs;
    }

    /// <summary>
    /// The buffs of the players and monsters of the maze, for whatever gives one outside the attacks of a skill.
    /// </summary>
    [LuaIgnoreMember]
    public MazeBuffs Buffs => _buffs;

    /// <summary>
    /// An attack for the side of the player gives its buff to the player and the party in its area; an attack whose only
    /// effect is a buff gives it to the monsters it reached. Neither hurts anybody. False for an attack that hurts.
    /// </summary>
    private bool GivesBuffOnly(Fighter member, in AttackTrigger trigger, Entity[] targets)
    {
        if (!trigger.ForAllies && !((trigger.Buff > 0 || trigger.Aura > 0) && trigger.DamageRate <= 0f))
        {
            return false;
        }

        if (!member.Gone && Ecs.IsAlive(member.Player))
        {
            _buffs.GiveFromAttack(member.Player, trigger, targets);
        }

        return true;
    }

    /// <summary>
    /// A script gives a buff to an actor of the maze, a player or a monster: for sure, with the actor as its owner.
    /// </summary>
    private bool ApplyScriptBuff(int actor, int buff)
    {
        foreach (var member in _members)
        {
            if (Ecs.IsAlive(member.Player) && Ecs.Get<ActorComponent>(member.Player) == actor)
            {
                return _buffs.Give(member.Player, buff, actor, roll: false);
            }
        }

        return creatureManager.FindMonster(((ActorComponent)actor).Identifier) is { } monster && _buffs.Give(monster, buff, actor, roll: false);
    }

    private void OnBuffKill(Entity monster, int owner)
    {
        Run(KillByBuffAsync(monster, owner));
    }

    /// <summary>
    /// A monster died of a buff: it counts as the kill of the player who gave the buff, or of the first player when the one
    /// who gave it is gone or was not a player.
    /// </summary>
    private async ValueTask KillByBuffAsync(Entity monster, int owner)
    {
        if (Closed || !Ecs.IsAlive(monster) || _members.Count == 0)
        {
            return;
        }

        var killer = _members[0].Player;

        foreach (var member in _members)
        {
            if (Ecs.IsAlive(member.Player) && Ecs.Get<ActorComponent>(member.Player) == owner)
            {
                killer = member.Player;
            }
        }

        var (identifier, place, fell) = RemoveDead(monster);

        MonsterKilled?.Invoke(killer, identifier, fell);

        if (Ecs.IsAlive(killer))
        {
            await reports.MonsterKilledAsync(killer, identifier, MazeReporter.GetKillConditions(0, false));
        }

        if (Closed)
        {
            return;
        }

        await State.OnDie(identifier, place.SpawnBox, this);

        if (!Closed)
        {
            await CheckSectorAsync(place.Sector);
        }
    }
}
