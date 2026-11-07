using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.DailyMissions;

namespace OpenWorker.Commands.DailyMissionCommands;

[CommandTrigger("dailymission", "draw")]
[CommandDescription("Draw the daily missions of the player again")]
internal sealed partial class DailyMissionDrawCommand : ICommand
{
    [ExternalDependency]
    private DailyMissionManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        await Manager.RedrawAsync(player, CancellationToken.None);

        return true;
    }
}
