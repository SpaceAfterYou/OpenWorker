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
/// The player presses an attack key while it controls a monster (17/52): the monster plays a skill of its row. The client
/// waits for no answer; a skill the row cannot have counts against the error budget.
/// </summary>
public sealed class ControlledMonsterService(World world, HandlerExecutor errors) : IHotspotHandler<MonsterControlAttackRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, MonsterControlAttackRequest request)
    {
        if (world.Has<LuaMaze>(context.Player) &&
            world.Get<LuaMaze>(context.Player).ControlAttack(context.Player, request.Monster, request.Attack) == ClaimVerdict.Refused)
        {
            errors.Charge(context.Player, MessageOpcode.From(GroupOpcode.Monster, MonsterOpcode.ControlMonsterAttackReq), HandlerErrorReason.Violation);
        }

        return ValueTask.CompletedTask;
    }
}
