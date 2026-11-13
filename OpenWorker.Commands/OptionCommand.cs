using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Commands;

/// <summary>
/// Switches a content of this server on or off, as <c>/option index true|false</c>. Everybody on the server is told at once:
/// the client in the game reads the server options only from 01/07.
/// </summary>
[CommandTrigger("option")]
[CommandDescription("Switch a content of this server on or off")]
internal sealed partial class OptionCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private ServerContent Contents { get; }

    [ExternalDependency]
    private CommunityNetwork Network { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var index) || index < 0 || index >= Contents.Count)
        {
            return ValueTask.FromResult(false);
        }

        if (!bool.TryParse(tokens.ElementAtOrDefault(1), out var flag))
        {
            return ValueTask.FromResult(false);
        }

        Contents[index] = flag;

        var update = new SystemServerOptionUpdateResponse { Contents = Contents };

        foreach (var person in Network.Online)
        {
            if (World.IsAlive(person) && World.TryGet(person, out ServerSessionComponent session))
            {
                session.Send(update);
            }
        }

        return ValueTask.FromResult(true);
    }
}
