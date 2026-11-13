using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Starts the unity mode of an event of the unity table in the sector the player stands in: <c>/unity-start 40101</c>.
/// </summary>
[CommandTrigger("unity", "start")]
[CommandDescription("Start the unity mode of an event in your sector")]
internal sealed partial class UnityStartCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) ||
            !int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var table) ||
            table is <= 0 or > ushort.MaxValue)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(World.Get<LuaMaze>(player).StartUnityModeFor(player, table));
    }
}
