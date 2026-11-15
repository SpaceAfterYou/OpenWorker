using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Commands;

[CommandTrigger("title-open")]
[CommandDescription("Open the titles a condition and a value open, such as the titles of an event (condition 9, value 0)")]
internal sealed partial class TitleOpenCommand : ICommand
{
    [ExternalDependency]
    private OpenTitleManager Titles { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var opened = byte.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var condition) &&
                     int.TryParse(tokens.ElementAtOrDefault(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) &&
                     Titles.Update(player, condition, value) > 0;

        return ValueTask.FromResult(opened);
    }
}
