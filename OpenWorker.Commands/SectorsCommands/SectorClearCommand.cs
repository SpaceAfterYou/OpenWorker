using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.SectorsCommands;

/// <summary>
/// Kills what lives in a sector of the maze and clears it, as if the player had done it: <c>/sector-clear [sector]</c>.
/// Without a number it is the sector the player stands in.
/// </summary>
[CommandTrigger("sector", "clear")]
[CommandDescription("Clear maze sector")]
internal sealed partial class SectorClearCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player))
        {
            // TODO: Not in maze message
            return false;
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(0), out var sector);

        await World.Get<LuaMaze>(player).ClearSectorAsync(sector);

        return true;
    }
}
