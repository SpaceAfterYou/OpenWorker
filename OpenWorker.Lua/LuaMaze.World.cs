using Lua;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Lua;

/// <summary>
/// An open maze of the world (District 6): everybody of a channel shares one instance. What it holds comes out at once when its
/// first player arrives, and nothing is ever cleared there.
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
}
