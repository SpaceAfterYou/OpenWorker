using Arch.Core;
using Lua;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;

namespace OpenWorker.Lua;

/// <summary>
/// An open maze of the world (District 6): everybody of a channel shares one instance. What it holds comes out at once when its
/// first player arrives, every monster of a box with a respawn time comes back that long after it falls, and nothing is ever
/// cleared there.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// The channel of the open maze of the world this instance plays, zero for any other maze.
    /// </summary>
    [LuaIgnoreMember]
    public byte World { get; init; }

    /// <summary>
    /// Whether the instance is an open maze of the world.
    /// </summary>
    [LuaIgnoreMember]
    public bool IsWorld => World != 0;

    /// <summary>
    /// Every box that brings its monsters at once does so, whatever sector the first player stands in.
    /// </summary>
    private void StartWorld()
    {
        if (!IsWorld)
        {
            return;
        }

        foreach (var box in Batch.EventBox.MonsterSpawns)
        {
            if (box.CreationCondition == CreationConditionType.Immediate)
            {
                creatureManager.SpawnOnce(box.Id);
            }
        }
    }

    /// <summary>
    /// A monster that fell comes back after the respawn time of its box (milliseconds on the game time of the maze); a box
    /// without one keeps it dead.
    /// </summary>
    private void RespawnLater(int box, int identifier)
    {
        if (!IsWorld || Batch.EventBox.MonsterSpawns.Find(box) is not { } spawn || !float.IsFinite(spawn.RespawnTime) || spawn.RespawnTime <= 0f)
        {
            return;
        }

        Later(spawn.RespawnTime / 1000f, () =>
        {
            creatureManager.Reappear(identifier, spawn);

            return ValueTask.CompletedTask;
        });
    }

    /// <summary>
    /// A player who gets up at its safe place stands at the start box it came into the map by, or at the first start of the map.
    /// </summary>
    [LuaIgnoreMember]
    public void ReturnToStart(Entity player)
    {
        if (!IsWorld || !Ecs.IsAlive(player) || !Ecs.TryGet(player, out WorldComponent place))
        {
            return;
        }

        var starts = Batch.EventBox.StartEvents;
        var start = place.Jump != 0 ? starts.Find(place.Jump) : null;

        start ??= starts.Count > 0 ? starts[0] : null;

        if (start is not null)
        {
            Ecs.Set(player, place with { Position = start.GetPosition(), Rotation = start.Rotation });
        }
    }
}
