using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.SectorsCommands;

/// <summary>
/// Brings a spawn box of the map to life, as if its condition had been met: <c>/spawn box 40590</c>.
/// </summary>
[CommandTrigger("spawn", "box")]
[CommandDescription("Spawn a monster box of the maze")]
internal sealed partial class SpawnBoxCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) || !int.TryParse(tokens.ElementAtOrDefault(0), out var box) || box <= 0)
        {
            return ValueTask.FromResult(false);
        }

        World.Get<LuaMaze>(player).SpawnBox(box);

        return ValueTask.FromResult(true);
    }
}
