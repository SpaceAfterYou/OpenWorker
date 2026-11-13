using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Monster.Requests;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// A player gets on a defensive weapon of the maze (17/41) and off it (17/43). The client waits for no answer to either: a
/// ride that starts or ends is told to everybody, and a claim the client cannot make counts against the error budget.
/// </summary>
public sealed class DefensiveWeaponService(World world, HandlerExecutor errors) :
    IHotspotHandler<MonsterWeaponMountRequest>,
    IHotspotHandler<MonsterWeaponDismountRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, MonsterWeaponMountRequest request)
    {
        if (world.Has<LuaMaze>(context.Player) && world.Get<LuaMaze>(context.Player).MountWeapon(context.Player, request.Weapon) == ClaimVerdict.Refused)
        {
            errors.Charge(context.Player, MessageOpcode.From(GroupOpcode.Monster, MonsterOpcode.DefensiveWeaponStartReq), HandlerErrorReason.Violation);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, MonsterWeaponDismountRequest request)
    {
        if (world.Has<LuaMaze>(context.Player))
        {
            world.Get<LuaMaze>(context.Player).DismountWeapon(context.Player, request.Weapon);
        }

        return ValueTask.CompletedTask;
    }
}
