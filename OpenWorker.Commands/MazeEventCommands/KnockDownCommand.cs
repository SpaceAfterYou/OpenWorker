using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// The nearest monster throws the player up with a blow that hurts it a little: <c>/knock-down</c>. The player flies, then lies on
/// the ground until it gets up.
/// </summary>
[CommandTrigger("knock", "down")]
[CommandDescription("Make the nearest monster throw you to the ground")]
internal sealed partial class KnockDownCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) || tokens.Length != 0)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(World.Get<LuaMaze>(player).KnockDown(player));
    }
}
