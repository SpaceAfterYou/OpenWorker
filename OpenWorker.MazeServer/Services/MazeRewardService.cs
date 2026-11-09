using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The player opens the key reward of the window shown at the end of the maze, naming the slot of its keys.
/// </summary>
public sealed class MazeRewardService(World world, MazeKeyRewards keys) : IHotspotHandler<ItemMazeRewardItemRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, ItemMazeRewardItemRequest request)
    {
        if (!world.IsAlive(context.Player) || !world.TryGet<ServerSessionComponent>(context.Player, out var session))
        {
            return ValueTask.CompletedTask;
        }

        if (keys.Take(context.Player, request.Storage, request.Slot) is { } code)
        {
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Item, ItemOpcode.MazeRewardItem), code));
        }
        else
        {
            session.Send(new ItemMazeRewardItemResponse());
        }

        return ValueTask.CompletedTask;
    }
}
