using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    // The boxes of the cubes that are taken by holding the click have this in the name of their object.
    private const string CubeKey = "VaccumCube";

    /// <summary>
    /// The objects of soul metries stand in the maze all the time, but only the ones the person is to find this
    /// time are shown. This is called before the client is told what the maze holds.
    /// </summary>
    public void HideUnfoundSoulObjects(Entity player)
    {
        var objects = reports.GetSoulObjects(info.Id);

        if (objects.Count == 0)
        {
            return;
        }

        var shown = reports.GetSoulObjectsToShow(player, info.Id);

        foreach (var box in Batch.EventBox.InterActions.Where(e => objects.Contains(e.Interaction) && !shown.Contains(e.Interaction)))
        {
            Progress.HideInteraction(box.Id);
        }
    }

    /// <summary>
    /// The person clicked an object of the maze: it counts for the quests, for everyone in the maze when the object says so,
    /// and if it is one of a soul metry, the soul metry finds it.
    /// </summary>
    public async ValueTask CollectSoulObjectAsync(Entity player, int box)
    {
        if (Progress.CountsForEveryone(box))
        {
            foreach (var member in _members.Select(e => e.Player).Where(Ecs.IsAlive).ToArray())
            {
                await reports.ObjectUsedAsync(member, box);

                if (Closed)
                {
                    return;
                }
            }
        }

        else
        {
            await reports.ObjectUsedAsync(player, box);
        }

        if (Closed)
        {
            return;
        }

        if (Batch.EventBox.InterActions.Find(box) is { } found)
        {
            await reports.CollectSoulObjectAsync(player, info.Id, found.Interaction);
        }
    }

    /// <summary>
    /// The cubes of the maze, which the person takes by holding the click for the time the table gives the object.
    /// </summary>
    public void RegisterCubes(Entity player)
    {
        foreach (var box in Batch.EventBox.InterActions.Where(e => e.ObjectKey.Contains(CubeKey, StringComparison.OrdinalIgnoreCase)))
        {
            var row = interactions.GetOrDefault(box.Interaction);

            reports.RegisterCube(player, box.Id, box.Interaction, TimeSpan.FromMilliseconds(row.ActionDelay));
        }
    }

    [LuaMember]
    public void EnableInteractionBox(int box, bool enable)
    {
        if (Progress.SetInteraction(box, enable) is { } value)
        {
            creatureManager.Broadcast(new MazeInteractionEnableResponse
            {
                Show = value.Show,
                Enable = value.Enable,
                BoxIndex = value.Box,
                CallCount = value.CallCount
            });
        }
    }
}
