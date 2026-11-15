using System.Numerics;
using System.Runtime.InteropServices;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Ai;

namespace OpenWorker.Lua.Managers;

public sealed partial class LuaCreatureManager
{
    private const string StandAnimation = "N_Stand";
    private const float DegreesToRadians = MathF.PI / 180f;

    /// <summary>
    /// A bone the animation leaves this near the feet is taken as not moved by it.
    /// </summary>
    private const float Unmoved = 0.00001f;

    private Dictionary<Entity, MonsterParts> Parts { get; } = [];

    /// <summary>
    /// The part each monster found by the last <see cref="MonstersStruck"/> was struck on, for the damage of that blow.
    /// </summary>
    private Dictionary<Entity, byte> StruckParts { get; } = [];

    /// <summary>
    /// Monsters a part of which has just broken, and what they turn into; they start the change at the next step.
    /// </summary>
    private Dictionary<Entity, PartBreak> Breaking { get; } = [];

    /// <summary>
    /// Monsters that play the animation of their change, the moment it is over and what they turn into then.
    /// </summary>
    private Dictionary<Entity, (double At, int Monster)> Changing { get; } = [];

    private readonly List<BonePlace> _bonePlaces = [];

    /// <summary>
    /// The monsters a blow over an area lands on, by the cylinder and the hit bones of each, and the part it lands on, kept for
    /// <see cref="StruckPart"/>.
    /// </summary>
    public Entity[] MonstersStruck(AttackArea area)
    {
        StruckParts.Clear();

        var struck = new List<Entity>();

        foreach (var creature in Creatures)
        {
            if (ecs.Get<ActorComponent>(creature).Type != ActorType.Monster || IsHidden(creature))
            {
                continue;
            }

            if (Strikes(creature, area, out var part))
            {
                struck.Add(creature);

                if (part != 0)
                {
                    StruckParts[creature] = part;
                }
            }
        }

        return [.. struck];
    }

    /// <summary>
    /// Whether a blow over an area lands on a monster, by its cylinder and its hit bones, and the part it lands on.
    /// </summary>
    internal bool Strikes(Entity monster, AttackArea area, out byte part)
    {
        var place = ecs.Get<WorldComponent>(monster);
        var (radius, height, hitBody) = HitCylinderOf(monster);

        PlaceBones(monster, ecs.Get<CreatureComponent>(monster).Identifier, hitBody, place);

        return area.Strikes(place.Position, radius, height, CollectionsMarshal.AsSpan(_bonePlaces), out part);
    }

    /// <summary>
    /// The part a blow over an area lands on for each of the monsters, in their order: zero where it lands on none.
    /// </summary>
    internal byte[] PartsStruck(IReadOnlyList<Entity> monsters, AttackArea area)
    {
        var parts = new byte[monsters.Count];

        for (var index = 0; index < monsters.Count; index++)
        {
            if (ecs.IsAlive(monsters[index]) && Strikes(monsters[index], area, out var part))
            {
                parts[index] = part;
            }
        }

        return parts;
    }

    /// <summary>
    /// A monster as something a projectile or a chain can run into: the cylinder blows land on, and its hit bones where they are now.
    /// </summary>
    internal FlightTarget FlightTargetOf(Entity monster, int key)
    {
        var place = ecs.Get<WorldComponent>(monster);
        var (radius, height, hitBody) = HitCylinderOf(monster);

        PlaceBones(monster, ecs.Get<CreatureComponent>(monster).Identifier, hitBody, place);

        return new FlightTarget(key, place.Position, radius, height, _bonePlaces.Count > 0 ? [.. _bonePlaces] : null);
    }

    /// <summary>
    /// The cylinder blows land on: that of the file beside the action file when it gives one, else that of the table.
    /// </summary>
    private (float Radius, float Height, HitBody? Body) HitCylinderOf(Entity monster)
    {
        var body = ecs.Get<MonsterCombatComponent>(monster);
        var hitBody = HitBodyOf(ecs.Get<CreatureComponent>(monster).Identifier);

        return hitBody is { HasCylinder: true } ? (hitBody.CylinderRadius, hitBody.CylinderHeight, hitBody) : (body.BodyRadius, body.BodyHeight, hitBody);
    }

    /// <summary>
    /// The part the last blow found the monster struck on; zero for none.
    /// </summary>
    public byte StruckPart(Entity monster) => StruckParts.GetValueOrDefault(monster);

    private HitBody? HitBodyOf(int identifier)
    {
        return Services is { } services && services.Behaviors.Get(identifier) is { } behavior ? services.Actions.GetHitBody(behavior.ActionFile) : null;
    }

    /// <summary>
    /// Where the hit bones of a monster are now: where the animation it plays puts them, turned the way it looks and scaled as it
    /// is drawn; where they rest on the model, not turned, when the animation does not move them.
    /// </summary>
    private void PlaceBones(Entity monster, int identifier, HitBody? hitBody, in WorldComponent place)
    {
        _bonePlaces.Clear();

        if (hitBody is not { Bones.Count: > 0 } || Services is not { } services)
        {
            return;
        }

        var (animation, time) = Bodies.TryGetValue(monster, out var mind)
            ? mind.HitAnimation()
            : (services.Behaviors.Get(identifier) is { } behavior ? services.Actions.GetAnimation(behavior.ActionFile, StandAnimation) : null, 0f);
        var scale = services.Parts?.ScaleOf(identifier) ?? 1f;
        var (sin, cos) = MathF.SinCos(place.Rotation * DegreesToRadians);

        for (var i = 0; i < hitBody.Bones.Count; i++)
        {
            var bone = hitBody.Bones[i];
            var moved = (animation?.Bones?.At(i, time) ?? Vector3.Zero) * scale;
            var center = MathF.Abs(moved.X) <= Unmoved && MathF.Abs(moved.Y) <= Unmoved && MathF.Abs(moved.Z) <= Unmoved
                ? place.Position + bone.Rest
                : place.Position + new Vector3(moved.X * cos - moved.Y * sin, moved.X * sin + moved.Y * cos, moved.Z);

            _bonePlaces.Add(new BonePlace(center, bone.Radius, bone.Part));
        }
    }

    /// <summary>
    /// A monster that has just appeared gets its parts, from the life it appeared with.
    /// </summary>
    private void GiveParts(Entity monster, int identifier)
    {
        if (Services?.Parts?.Create(identifier, ecs.Get<CreatureComponent>(monster).Health) is { Any: true } parts)
        {
            Parts[monster] = parts;
        }
    }

    private void ForgetParts(Entity monster)
    {
        Parts.Remove(monster);
        StruckParts.Remove(monster);
        Breaking.Remove(monster);
        Changing.Remove(monster);
    }

    /// <summary>
    /// The parts of a monster, none for one without.
    /// </summary>
    public MonsterParts PartsOf(Entity monster) => Parts.GetValueOrDefault(monster);

    /// <summary>
    /// A blow lands on a monster with parts: a part the blow is on takes the damage and the clients are told the life of the
    /// parts; the life of the monster is never touched. A part that breaks changes the monster.
    /// </summary>
    /// <returns>Whether the monster has parts, so that its own life takes nothing.</returns>
    public bool TakeOnParts(Entity monster, byte part, int damage)
    {
        if (!Parts.TryGetValue(monster, out var parts))
        {
            return false;
        }

        parts = MonsterPartRules.Hit(parts, part, damage, out var struck);

        if (!struck)
        {
            return true;
        }

        Parts[monster] = parts;

        Broadcast(PartsMessage(monster, parts));

        if (!Breaking.ContainsKey(monster) && !Changing.ContainsKey(monster) && Services?.Parts?.BreakOf(parts) is { } broken)
        {
            Breaking[monster] = broken;
        }

        return true;
    }

    private MonsterPartsHpResponse PartsMessage(Entity monster, in MonsterParts parts)
    {
        return new MonsterPartsHpResponse
        {
            Actor = ecs.Get<ActorComponent>(monster),
            FirstCurrent = parts.First.Health,
            FirstMax = parts.First.MaxHealth,
            SecondCurrent = parts.Second.Health,
            SecondMax = parts.Second.MaxHealth
        };
    }

    /// <summary>
    /// The monsters whose part broke play the animation of their change, untouchable; the ones whose animation is over are
    /// changed.
    /// </summary>
    private void StepParts(double now)
    {
        if (Breaking.Count > 0)
        {
            foreach (var (monster, broken) in Breaking.ToArray())
            {
                Breaking.Remove(monster);

                if (!ecs.IsAlive(monster))
                {
                    continue;
                }

                var length = Bodies.TryGetValue(monster, out var body) ? body.PlayChange(broken.Animation) : 0f;

                SetInvincible(monster, true);

                Changing[monster] = (now + length, broken.Monster);
            }
        }

        if (Changing.Count == 0)
        {
            return;
        }

        foreach (var (monster, (at, next)) in Changing.ToArray())
        {
            if (now >= at)
            {
                Changing.Remove(monster);

                if (ecs.IsAlive(monster))
                {
                    ChangeMonster(monster, next);
                }
            }
        }
    }

    /// <summary>
    /// The monster is taken away and another appears in its place, facing the same way, with the life and the super armor it had
    /// and in its sector and box. The clients see the first go and the second come, then the parts and the armor of the second.
    /// Nobody killed the first: it gives nothing and its script is not told.
    /// </summary>
    private Entity ChangeMonster(Entity monster, int next)
    {
        var place = ecs.Get<WorldComponent>(monster);
        var origin = ecs.Get<NonPlayableCreatureComponent>(monster);
        var health = ecs.Get<CreatureComponent>(monster).Health;
        var armor = ecs.Get<MonsterCombatComponent>(monster).SuperArmor;
        AggroGroup? group = Bodies.TryGetValue(monster, out var body) ? body.Group : null;
        var actor = ecs.Get<ActorComponent>(monster);

        var created = CreateMonster(next, MonsterSpawnBox.At(next, place.Position, place.Rotation, origin.Sector));

        ecs.Set(created, ecs.Get<NonPlayableCreatureComponent>(created) with { SpawnBox = origin.SpawnBox, Waypoint = origin.Waypoint });

        var state = ecs.Get<CreatureComponent>(created);
        var combat = ecs.Get<MonsterCombatComponent>(created);

        ecs.Set(created, state with { Health = Math.Clamp(health, 1, Math.Max(1, (int)combat.Stats.MaxHealth)) });

        if (combat.MaxSuperArmor > 0f)
        {
            ecs.Set(created, combat with { SuperArmor = Math.Clamp(armor, 0f, combat.MaxSuperArmor) });
        }

        if (group is { } aggro && Bodies.TryGetValue(created, out var mind))
        {
            mind.SetGroup(aggro);
        }

        Broadcast(new WorldOutInfoMonsterResponse { Actors = [actor] });

        Remove(monster);

        Show([created]);

        var after = ecs.Get<MonsterCombatComponent>(created);

        Broadcast(PartsMessage(created, PartsOf(created)));
        Broadcast(new MonsterSuperArmorGageResponse { Actor = ecs.Get<ActorComponent>(created), Current = after.SuperArmor, Max = after.MaxSuperArmor });

        return created;
    }
}
