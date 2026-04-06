using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Gameplay.Mapping;

public static class MonsterSnapshotMapper
{
    public static STMonsterInfo ToStMonsterInfo(World world, Arch.Core.Entity item)
    {
        var creature = world.Get<CreatureComponent>(item);
        var nonPlayable = world.Get<NonPlayableCreatureComponent>(item);

        return new STMonsterInfo
        {
            stNpcInfo = new STNpcInfo
            {
                uxActorID = world.Get<ActorComponent>(item),
                stPosInfo = WorldPosition(world, item),
                nHP = 250,
                nWayPointID = nonPlayable.Waypoint,
                nSectorID = nonPlayable.Sector,
                byLevel = creature.Level,
                nTableID = creature.Identifier
            },
            uxParentActorID = default,
            nSpawnBoxID = nonPlayable.SpawnBox,
            nMotionClass = 0,
            bBattlePos = true,
            fCurSuperArmor = 100.0f,
            fMaxSuperArmor = 100.0f,
            vecStat = []
        };
    }

    private static WorldValue WorldPosition(World world, Arch.Core.Entity item)
    {
        var w = world.Get<WorldComponent>(item);
        return new WorldValue(w.Location, w.Map, w.Position, w.Rotation);
    }
}
