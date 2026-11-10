using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// Somebody a monster can fight, as the monster sees them.
/// </summary>
/// <param name="Id">The actor in the form the client keeps it in a packet.</param>
/// <param name="Yaw">Where the target looks, in degrees.</param>
public readonly record struct AiTarget(int Id, Vector3 Position, float Radius, float Yaw, int Health, int MaxHealth, bool IsMoving);

/// <summary>
/// A skill of a monster, with what the AI needs to know about it. Times are in seconds, distances in the units of the world.
/// </summary>
/// <param name="Angle">How far from the target it may look when the skill starts, in degrees.</param>
/// <param name="IgnoresRotation">The skill starts whatever way the monster looks.</param>
/// <param name="TargetsOwner">The skill is for the one who owns the monster and not for its enemy.</param>
public sealed record AiSkill(
    int Id,
    float RangeMin,
    float RangeMax,
    float Cooldown,
    int CooldownGroup,
    float GlobalCooldown,
    float Angle,
    bool IgnoresRotation,
    bool TargetsOwner);

/// <summary>
/// What a monster is made of and what it can do in the world. The machine decides, the host does and tells what it sees.
/// </summary>
public interface IAiHost
{
    Vector3 Position { get; }

    /// <summary>
    /// Where the monster came to life, it returns there when it loses its enemy.
    /// </summary>
    Vector3 HomePosition { get; }

    float Yaw { get; }
    float Radius { get; }
    int Health { get; }
    int MaxHealth { get; }
    bool IsDead { get; }

    /// <summary>
    /// Free to walk: nothing holds it, it is not hit, not in the middle of an attack.
    /// </summary>
    bool CanMove { get; }

    /// <summary>
    /// Free to start a skill.
    /// </summary>
    bool CanAttack { get; }

    bool IsMoving { get; }

    /// <summary>
    /// Can never walk: it turns and attacks what comes close.
    /// </summary>
    bool IsRooted { get; }

    /// <summary>
    /// In the middle of a skill.
    /// </summary>
    bool IsAttacking { get; }

    bool IsBattlePose { get; }

    /// <summary>
    /// The skill in a slot, counted from zero, or nothing.
    /// </summary>
    AiSkill? GetSkill(int slot);

    /// <summary>
    /// Somebody who is still in the fight, or nothing when they are gone or dead.
    /// </summary>
    AiTarget? GetTarget(int id);

    /// <summary>
    /// The enemy nearest to the monster, measured between bodies, if any is closer than the distance.
    /// </summary>
    AiTarget? FindEnemy(float distance);

    /// <summary>
    /// How many enemies stand within the distance of the monster.
    /// </summary>
    int CountEnemies(float distance);

    /// <summary>
    /// How many players stand within the distance, whatever side they are on.
    /// </summary>
    int CountUsers(float distance) => CountEnemies(distance);

    /// <summary>
    /// How many monsters of the sector the monster lives in are alive.
    /// </summary>
    int CountSectorMonsters();

    /// <summary>
    /// How many monsters this one has summoned that are alive.
    /// </summary>
    int CountSpawnedMonsters();

    /// <summary>
    /// How long the monster has been alive, in seconds.
    /// </summary>
    float Age { get; }

    void StopMoving(bool send);

    /// <summary>
    /// Sets off for a place. Nothing happens when the way is closed.
    /// </summary>
    bool MoveTo(Vector3 destination, bool run);

    void FaceTo(Vector3 point);
    void SetBattlePose(bool battle, bool playMotion);

    /// <summary>
    /// The monster looks at somebody else. The id is -1 for nobody.
    /// </summary>
    void SetTarget(int id);

    /// <summary>
    /// Circles its target, or stops doing it.
    /// </summary>
    void SetGaze(bool gazing);

    /// <summary>
    /// Starts a skill against the target. False when it cannot.
    /// </summary>
    bool UseSkill(AiSkill skill, AiTarget target);

    /// <summary>
    /// Plays an animation of its own. Returns how long it lasts, or zero when the monster has no such animation.
    /// </summary>
    float PlayAction(string name);

    void SetInvincible(bool invincible);
    void RestoreHealth();

    /// <summary>
    /// Takes every buff and debuff away from the monster: one that is getting back on its feet starts over.
    /// </summary>
    void ClearBuffs();

    /// <summary>
    /// Puts the monster on a place and tells everybody.
    /// </summary>
    void Teleport(Vector3 position);

    void Die();

    /// <summary>
    /// The person a helper follows and fights for, or nothing for a monster of the maze.
    /// </summary>
    AiTarget? Owner => null;

    /// <summary>
    /// Whether there is ground to stand on at a place, near its height.
    /// </summary>
    bool HasGround(Vector3 position) => true;

    /// <summary>
    /// A helper is put where its owner stands, and everybody is told where it is now.
    /// </summary>
    void WarpTo(Vector3 position) => Teleport(position);

    /// <summary>
    /// Gets back some life, never above the maximum.
    /// </summary>
    void Heal(int amount)
    {
    }

    /// <summary>
    /// A skill by its number, whether or not it is in one of the slots: the skills a script names for getting up and
    /// for escaping.
    /// </summary>
    AiSkill? FindSkill(int id) => null;
}
