using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Skill;

namespace OpenWorker.Commands.Persons;

/// <summary>
/// Gives the player a skill of its hero at any level and puts it first in a position of the deck, then tells the client its
/// skills again: <c>/skill-learn 32000711 2</c> (the position counts from 0; 0 without one).
/// </summary>
[CommandTrigger("skill", "learn")]
[CommandDescription("Learn a skill of the hero at any level and put it in a position of the deck")]
internal sealed partial class SkillLearnCommand : ICommand
{
    [ExternalDependency]
    private SkillLearning Learning { get; }

    [ExternalDependency]
    private PersonSkills Skills { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var position = 0;

        if (tokens.Length is < 1 or > 2 ||
            !int.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out var skill) ||
            (tokens.Length == 2 && !int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out position)) ||
            !Learning.Grant(player, skill, position))
        {
            return ValueTask.FromResult(false);
        }

        Skills.Send(player);

        return ValueTask.FromResult(true);
    }
}
