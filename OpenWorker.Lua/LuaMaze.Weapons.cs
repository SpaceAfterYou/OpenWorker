using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// Defensive weapons: monsters of the maze that a player gets on (17/41, told to everybody with 17/42) and fires with its attack
/// keys (17/45) until it gets off (17/43, told with 17/44) or leaves the maze. Nothing can hurt the weapon, and nothing hurts the
/// player while it is on one; its own skills wait until it is off.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// How near the weapon the player must stand to get on it, as its client measures it.
    /// </summary>
    internal const float WeaponReach = 300f;

    /// <summary>
    /// What the place the server keeps of a player may lag behind the place its client measured from.
    /// </summary>
    private const float WeaponReachSlack = 150f;

    /// <summary>
    /// The weapon fires from no motion of its own.
    /// </summary>
    private const short WeaponNoMotion = -1;

    /// <summary>
    /// The weapon a player is on.
    /// </summary>
    private sealed class Ride(ActorValue weapon)
    {
        public ActorValue Weapon { get; } = weapon;

        /// <summary>
        /// Until when the skill it fires lasts: it fires no other before.
        /// </summary>
        public TimeSpan BusyUntil { get; set; }
    }

    private readonly Dictionary<Fighter, Ride> _rides = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The player gets on the weapon. A player that is down, already on a weapon, or asks for a weapon somebody else is on or
    /// that has gone, gets nothing; a monster that is no weapon, or one farther than the client lets a player reach, is a claim
    /// the client cannot make.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict MountWeapon(Entity player, int weapon)
    {
        if (!Started || Find(player) is not { Loaded: true } member || _rides.ContainsKey(member) || !creatureManager.Vitals.IsAlive(player))
        {
            return ClaimVerdict.Dropped;
        }

        if (creatureManager.FindMonster(((ActorValue)weapon).Identifier) is not { } found || (int)Ecs.Get<ActorComponent>(found) != weapon)
        {
            return ClaimVerdict.Dropped;
        }

        if (!Ecs.Get<MonsterCombatComponent>(found).IsDefensiveWeapon)
        {
            return ClaimVerdict.Refused;
        }

        if (_rides.Values.Any(e => (int)e.Weapon == weapon))
        {
            return ClaimVerdict.Dropped;
        }

        if (Vector3.Distance(Ecs.Get<WorldComponent>(player).Position, Ecs.Get<WorldComponent>(found).Position) > WeaponReach + WeaponReachSlack)
        {
            return ClaimVerdict.Refused;
        }

        var actor = Ecs.Get<ActorComponent>(found);

        _rides[member] = new Ride(actor);

        creatureManager.Vitals.Protect(player, LongestWait);

        creatureManager.Broadcast(new MonsterWeaponMountResponse { Rider = Ecs.Get<ActorComponent>(player), Weapon = actor });

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// The player gets off its weapon. A weapon it is not on is a request that came after the ride had ended.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict DismountWeapon(Entity player, int weapon)
    {
        if (Find(player) is not { } member || !_rides.TryGetValue(member, out var ride) || (int)ride.Weapon != weapon)
        {
            return ClaimVerdict.Dropped;
        }

        EndRide(member);

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// The player's attack key makes its weapon fire a skill of its row: the first key the first skill, the second the second.
    /// A weapon the player is not on, one that has gone, or one that still fires its last skill does nothing; a skill the row
    /// cannot have is a claim the client cannot make. The weapon fires from where it stands, the way its rider faces, with its
    /// own stats; what it kills counts for the rider.
    /// </summary>
    [LuaIgnoreMember]
    public ClaimVerdict FireWeapon(Entity player, int weapon, byte attack)
    {
        if (!Started || Find(player) is not { } member || !_rides.TryGetValue(member, out var ride) || (int)ride.Weapon != weapon)
        {
            return ClaimVerdict.Dropped;
        }

        if (!creatureManager.Vitals.IsAlive(player) || creatureManager.FindMonster(ride.Weapon.Identifier) is not { } found)
        {
            EndRide(member);

            return ClaimVerdict.Dropped;
        }

        var behavior = creatureManager.Services?.Behaviors.Get(Ecs.Get<CreatureComponent>(found).Identifier);

        if (behavior is null || attack >= behavior.Skills.Count || behavior.Skills[attack] == 0)
        {
            return ClaimVerdict.Refused;
        }

        if (Clock.Time < ride.BusyUntil)
        {
            return ClaimVerdict.Dropped;
        }

        var skill = behavior.Skills[attack];
        var hits = actions.GetHits(skill, behavior.ActionFile);
        var length = creatureManager.Services?.Behaviors.GetSkillDuration(skill) ?? 0f;

        ride.BusyUntil = Clock.Time + Wait(Math.Max(length, hits.Count > 0 ? hits[^1].Time : 0f));

        var place = Ecs.Get<WorldComponent>(found).Position;
        var yaw = Ecs.Get<WorldComponent>(player).Rotation;
        var position = new SkillPositionValue { Position = place, Angle = yaw, MotionClass = WeaponNoMotion };

        creatureManager.Broadcast(new SkillActiveBroadcastResponse
        {
            Skill = default(ActiveSkillValue) with
            {
                Actor = ride.Weapon,
                Skill = skill,
                Position = position,
                RandomTrapIndex = creatureManager.Random.Next(1, int.MaxValue)
            }
        });

        foreach (var hit in hits)
        {
            _schedule.Schedule(Wait(hit.Time), () => WeaponStrike(member, ride, skill, hit.Trigger, position));
        }

        return ClaimVerdict.Taken;
    }

    /// <summary>
    /// One blow of the weapon. A weapon its rider has left, or one that has gone, strikes no more.
    /// </summary>
    private void WeaponStrike(Fighter member, Ride ride, int skill, AttackTrigger trigger, SkillPositionValue position)
    {
        if (Closed || member.Gone || !_rides.TryGetValue(member, out var now) || now != ride || !creatureManager.Vitals.IsAlive(member.Player) ||
            creatureManager.FindMonster(ride.Weapon.Identifier) is not { } weapon)
        {
            return;
        }

        var place = Ecs.Get<WorldComponent>(weapon).Position;
        var yaw = Ecs.Get<WorldComponent>(member.Player).Rotation;
        var area = new AttackArea(trigger, place, yaw);
        var targets = creatureManager.FindMonsters((at, body) => area.Touches(at, body.BodyRadius, body.BodyHeight));

        Run(ApplyDamageAsync(member, skill, trigger, targets, position, origin: place, card: new CardStrike(ride.Weapon, place, yaw),
            strength: Ecs.Get<MonsterCombatComponent>(weapon).Stats));
    }

    /// <summary>
    /// Whether the player is on a defensive weapon: its own skills wait until it is off.
    /// </summary>
    [LuaIgnoreMember]
    public bool IsOnWeapon(Entity player) => Find(player) is { } member && _rides.ContainsKey(member);

    /// <summary>
    /// A player who comes in sees who is on which weapon.
    /// </summary>
    [LuaIgnoreMember]
    public void ShowRidesTo(Entity player)
    {
        if (!TrySession(player, out var session))
        {
            return;
        }

        foreach (var (member, ride) in _rides)
        {
            if (member.Player != player && Ecs.IsAlive(member.Player))
            {
                session.Send(new MonsterWeaponMountResponse { Rider = Ecs.Get<ActorComponent>(member.Player), Weapon = ride.Weapon });
            }
        }
    }

    /// <summary>
    /// The player is off its weapon: everybody is told, and it can be hurt again.
    /// </summary>
    private void EndRide(Fighter member)
    {
        if (!_rides.Remove(member, out var ride) || Closed || !Ecs.IsAlive(member.Player) || !Ecs.Has<ActorComponent>(member.Player))
        {
            return;
        }

        creatureManager.Vitals.Protect(member.Player, TimeSpan.Zero);

        creatureManager.Broadcast(new MonsterWeaponDismountResponse { Rider = Ecs.Get<ActorComponent>(member.Player), Weapon = ride.Weapon });
    }

    /// <summary>
    /// A weapon the maze no longer has carries nobody.
    /// </summary>
    private void StepRides()
    {
        if (_rides.Count == 0)
        {
            return;
        }

        foreach (var (member, ride) in _rides.ToArray())
        {
            if (creatureManager.FindMonster(ride.Weapon.Identifier) is null)
            {
                EndRide(member);
            }
        }
    }
}
