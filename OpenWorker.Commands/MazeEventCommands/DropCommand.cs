using System.Globalization;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// Lets an item fall at the feet of the player as a kill lets its drop fall: <c>/drop 650000001 [count]</c>. Only in a maze, and
/// only an item the client has.
/// </summary>
[CommandTrigger("drop")]
[CommandDescription("Let an item fall at your feet as a kill would")]
internal sealed partial class DropCommand : ICommand
{
    private const int MaxCount = 999;

    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private ItemCatalog Items { get; }

    // Only the maze server keeps what lies on the ground.
    [ExternalDependency]
    private IServiceProvider Services { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var count = 1;

        if (!World.Has<LuaMaze>(player) || Services.GetService<DropService>() is not { } drops ||
            !int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var item) || !Items.Exists(item) ||
            tokens.Length > 2 ||
            tokens.Length == 2 && (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count is < 1 or > MaxCount))
        {
            return ValueTask.FromResult(false);
        }

        drops.Place(player, [new DropThing(item, count)], World.Get<WorldComponent>(player).Position);

        return ValueTask.FromResult(true);
    }
}
