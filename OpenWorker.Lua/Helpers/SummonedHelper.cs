using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Helpers.Types;

namespace OpenWorker.Lua.Helpers;

/// <summary>
/// A helper a person has called into the maze: a monster of its own table row that follows the person and fights for it.
/// </summary>
public sealed class SummonedHelper
{
    internal SummonedHelper(Entity entity, ActorComponent actor, Entity owner, ActorComponent ownerActor, HelperValue record, byte faction)
    {
        Entity = entity;
        Actor = actor;
        Owner = owner;
        OwnerActor = ownerActor;
        Record = record;
        Faction = faction;
    }

    public Entity Entity { get; }

    public ActorComponent Actor { get; }

    /// <summary>
    /// The person the helper belongs to.
    /// </summary>
    public Entity Owner { get; }

    public ActorComponent OwnerActor { get; }

    /// <summary>
    /// The helper as the client reads it, called.
    /// </summary>
    public HelperValue Record { get; }

    /// <summary>
    /// The helper, which is the id of its monster row.
    /// </summary>
    public int Helper => Record.Helper;

    /// <summary>
    /// The side the helper is on: the monsters of the sides its row is at war with are its enemies.
    /// </summary>
    public byte Faction { get; }

    internal HelperBody? Body { get; set; }

    /// <summary>
    /// Whether the helper has been beaten down and is getting up: nothing can hurt it then.
    /// </summary>
    public bool IsRecovering => Body?.Machine.IsRecovering == true;
}

/// <summary>
/// A blow of a helper: the monsters it lands on take it through the damage of the maze, and what they drop is the owner's.
/// </summary>
internal readonly record struct HelperStrike(SummonedHelper Helper, int Skill, AttackTrigger Trigger, Entity[] Targets);
