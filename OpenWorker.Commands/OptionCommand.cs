using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Stores;
using OpenWorker.Hotspot.Cache;

namespace OpenWorker.Commands;

/// <summary>
/// Switches a content of the game on or off on every server, as <c>/option index true|false</c>. The table is written, so a
/// server that starts later has it too, and then every server takes the switch and tells its players.
/// </summary>
[CommandTrigger("option")]
[CommandDescription("Switch a content of the game on or off on every server")]
internal sealed partial class OptionCommand : ICommand
{
    [ExternalDependency]
    private IServerContentStore Store { get; }

    [ExternalDependency]
    private ServerContentChanges Changes { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!byte.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || !Enum.IsDefined((ServerContentIndex)number))
        {
            return false;
        }

        if (!bool.TryParse(tokens.ElementAtOrDefault(1), out var flag))
        {
            return false;
        }

        var index = (ServerContentIndex)number;

        await Store.SetAsync(index, flag);
        await Changes.SwitchAsync(index, flag);

        return true;
    }
}
