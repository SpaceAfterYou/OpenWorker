using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Gameplay.Mapping;

public static class MonsterSnapshotMapper
{
    /// <param name="battlePose">Whether the monster stands ready to fight.</param>
    public static STMonsterInfo ToStMonsterInfo(World world, Arch.Core.Entity item, bool battlePose = true)
    {
        var creature = world.Get<CreatureComponent>(item);
        var nonPlayable = world.Get<NonPlayableCreatureComponent>(item);
        var combat = world.Get<MonsterCombatComponent>(item);

        return new STMonsterInfo
        {
            stNpcInfo = new STNpcInfo
            {
                uxActorID = world.Get<ActorComponent>(item),
                stPosInfo = WorldPosition(world, item),
                nHP = creature.Health,
                nWayPointID = nonPlayable.Waypoint,
                nSectorID = nonPlayable.Sector,
                byLevel = creature.Level,
                nTableID = creature.Identifier
            },
            uxParentActorID = default,
            nSpawnBoxID = nonPlayable.SpawnBox,
            nMotionClass = 0,
            bBattlePos = battlePose,
            fCurSuperArmor = combat.SuperArmor,
            fMaxSuperArmor = combat.MaxSuperArmor,
            vecStat = combat.Stats.ToStatList()
        };
    }

    private static WorldValue WorldPosition(World world, Arch.Core.Entity item)
    {
        var w = world.Get<WorldComponent>(item);
        return new WorldValue(w.Location, w.Map, w.Position, w.Rotation);
    }
}
