using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Commands.Persons;

/// <summary>
/// Puts the player on a level, up or down, as if it had been gained: <c>/level 25</c>.
/// </summary>
[CommandTrigger("level")]
[CommandDescription("Set the player's level, with its stats and skill points")]
internal sealed partial class PersonLevelCommand : ICommand
{
    [ExternalDependency]
    private ExperienceService Experience { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (tokens.Length != 1 || !int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(Experience.SetLevel(player, level));
    }
}
