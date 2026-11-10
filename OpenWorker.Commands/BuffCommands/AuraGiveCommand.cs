using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.BuffCommands;

/// <summary>
/// Puts an aura of the aura table on the player in the maze, or takes it away: <c>/aura-give [aura]</c>, and without an aura
/// (or with 0) the aura the player has goes.
/// </summary>
[CommandTrigger("aura", "give")]
[CommandDescription("Put an aura on yourself, or take yours away")]
internal sealed partial class AuraGiveCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var aura = 0;

        if (!World.Has<LuaMaze>(player) ||
            (tokens.Length > 0 && !int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out aura)))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(World.Get<LuaMaze>(player).Buffs.SetAura(player, aura));
    }
}
