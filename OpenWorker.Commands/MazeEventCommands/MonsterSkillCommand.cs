using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// The monsters near the player that have a skill play it at the player now: <c>/monster-skill 1303053064</c>. They think no more
/// after it, so a still one stays where it was called and plays each skill it is told.
/// </summary>
[CommandTrigger("monster", "skill")]
[CommandDescription("Make the monsters near you play a skill of theirs at you")]
internal sealed partial class MonsterSkillCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) || tokens.Length != 1 ||
            !int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var skill) || skill <= 0)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(World.Get<LuaMaze>(player).PlayMonsterSkill(player, skill));
    }
}
