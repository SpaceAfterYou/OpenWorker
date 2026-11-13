using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Monster.Responses;

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
        if (!Ecs.IsAlive(player) || !Ecs.TryGet(player, out ServerSessionComponent session))
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
