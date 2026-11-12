using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Tower;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.InfiniteTower.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The way on from a cleared stage of the tower: into the maze of the next stage of the chapter, or, after the last stage,
/// out of the tower to the district (<see cref="InfiniteTower"/>). A stage that is not cleared, or a maze that is no stage,
/// is refused with the code of the reference.
/// </summary>
public sealed class InfiniteTowerService(World world, InfiniteTower tower, WorldManager manager, MazeInstances instances, MazeService mazes) :
    IHotspotHandler<InfiniteTowerEnterNextStageRequest>
{
    private static readonly MessageOpcode Request = MessageOpcode.From(GroupOpcode.InfiniteTower, InfiniteTowerOpcode.EnterNextStage);

    public async ValueTask OnHandleAsync(ServiceHandleContext context, InfiniteTowerEnterNextStageRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        if (!world.TryGet(context.Player, out LuaMaze? maze) || maze is null)
        {
            session.Send(new ErrorMessageResponse(Request, InfiniteTower.NotInMaze));

            return;
        }

        if (!maze.IsComplete || !tower.TryGetStage(maze.Maze, out var stage))
        {
            session.Send(new ErrorMessageResponse(Request, InfiniteTower.StageNotCleared));

            return;
        }

        // The last stage of a chapter leads out of the tower.
        if (stage.NextMaze == 0)
        {
            await mazes.LeaveMazeAsync(context.Player, 0);

            return;
        }

        if (!await manager.TryEnterMaze(context.Player, stage.NextMaze))
        {
            if (world.IsAlive(context.Player))
            {
                session.Send(new ErrorMessageResponse(Request, InfiniteTower.InvalidData));
            }

            return;
        }

        // The person is on the way to the next stage: the maze of this one is over for them.
        await instances.LeaveAsync(context.Player);
    }
}
