using Lua;
using OpenWorker.Batch.Entities;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// How long a townsman takes to turn, after which a script that waits for it hears of it.
    /// </summary>
    private const float NpcMoveSeconds = 1f;

    /// <summary>
    /// The most switches a maze keeps the state of.
    /// </summary>
    private const int MaxSwitchNpcs = 64;

    private Dictionary<int, bool> SwitchNpcs { get; } = [];

    /// <summary>
    /// The latest walk of each townsman, by its table id: a townsman sent somewhere else on the way arrives only there.
    /// </summary>
    private Dictionary<int, int> NpcWalks { get; } = [];

    [LuaMember]
    public void ChangeNpcAnimation(int npc, string animation)
    {
        NotDone("ChangeNpcAnimation");
        Logger.IgnoredChangeNpcAnimation(npc, animation);
    }

    /// <summary>
    /// A townsman walks to a point of the map, and the script hears when it is there (<c>OnNpcWayPoint</c>). It walks
    /// straight, and only the latest walk it was given ends.
    /// </summary>
    [LuaMember]
    public void MoveNpcToWayPoint(int npc, int waypoint)
    {
        Logger.CallMoveNpcToWayPoint(npc, waypoint);

        if (!creatureManager.TryFindCreature(npc, out var creature))
        {
            Logger.UnknownNpcToMove(npc, waypoint);
            return;
        }

        if (FindWayPoint(waypoint) is not { } point)
        {
            Logger.UnknownWayPoint(npc, waypoint);
            return;
        }

        var target = (point.PosTopLeft + point.PosBottomRight) / 2f;
        var walk = NpcWalks[npc] = NpcWalks.GetValueOrDefault(npc) + 1;
        var seconds = creatureManager.Walk(creature, target);

        Later(seconds, () =>
        {
            if (NpcWalks.GetValueOrDefault(npc) != walk || !Ecs.IsAlive(creature))
            {
                return ValueTask.CompletedTask;
            }

            creatureManager.Arrive(creature, target);

            return State.OnNpcWayPoint(npc, waypoint, this);
        });
    }

    private WayPoint? FindWayPoint(int waypoint)
    {
        foreach (var point in Batch.EventPoint.WayPoints)
        {
            if (point.Id == waypoint)
            {
                return point;
            }
        }

        return null;
    }

    /// <summary>
    /// The townsman turns to the angle, and the script hears when it has (<c>OnNpcRotation</c>) a moment later.
    /// </summary>
    [LuaMember]
    public void SetNpcRotation(int npc, float angle, int key)
    {
        if (float.IsFinite(angle) && creatureManager.TryFindCreature(npc, out var creature))
        {
            creatureManager.Turn(creature, angle);
        }

        Later(NpcMoveSeconds, () => State.OnNpcRotation(npc, key, this));
    }

    /// <summary>
    /// A townsman that works as a switch stands one way or the other, and a click turns it over.
    /// </summary>
    [LuaIgnoreMember]
    public void ClickSwitchNpc(int npc)
    {
        // The number comes from the client: a maze has a handful of switches, and no more states than that are kept.
        if (!SwitchNpcs.ContainsKey(npc) && SwitchNpcs.Count >= MaxSwitchNpcs)
        {
            return;
        }

        var status = !SwitchNpcs.GetValueOrDefault(npc);

        SwitchNpcs[npc] = status;

        creatureManager.Broadcast(new MazeSwitchNpcStatusResponse { Npc = npc, Status = status ? (byte)1 : (byte)0 });
    }
}
