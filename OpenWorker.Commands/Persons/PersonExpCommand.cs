using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Commands.Persons;

[CommandTrigger("exp")]
[CommandDescription("Give the player experience, raising its level as a kill would")]
internal sealed partial class PersonExpCommand : ICommand
{
    [ExternalDependency]
    private ExperienceService Experience { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return ValueTask.FromResult(false);
        }

        Experience.Add(player, value);

        return ValueTask.FromResult(true);
    }
}
