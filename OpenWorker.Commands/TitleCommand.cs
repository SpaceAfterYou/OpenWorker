using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;

namespace OpenWorker.Commands;

[CommandTrigger("title")]
[CommandDescription("Give the player a title by its number, or every title they can use with all")]
internal sealed partial class TitleCommand : ICommand
{
    [ExternalDependency]
    private TitleBook Titles { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var what = tokens.ElementAtOrDefault(0);

        if (string.Equals(what, "all", StringComparison.OrdinalIgnoreCase))
        {
            return await Titles.GrantAllAsync(player) > 0;
        }

        return int.TryParse(what, out var title) && await Titles.GrantAsync(player, title, TitleSource.Cheat);
    }
}
