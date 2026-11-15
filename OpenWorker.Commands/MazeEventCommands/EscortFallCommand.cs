using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// The escorted townsman of the maze falls where it is: <c>/escort-fall</c>. Its fall fails the quest of the party as any
/// fall would.
/// </summary>
[CommandTrigger("escort", "fall")]
[CommandDescription("Make the escorted townsman of your maze fall")]
internal sealed partial class EscortFallCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        return ValueTask.FromResult(tokens.Length == 0 && World.Has<LuaMaze>(player) && World.Get<LuaMaze>(player).FellEscorted(player));
    }
}
