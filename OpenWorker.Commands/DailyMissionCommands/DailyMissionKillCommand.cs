using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.DailyMissions;

namespace OpenWorker.Commands.DailyMissionCommands;

[CommandTrigger("dailymission", "kill")]
[CommandDescription("Count a kill for the daily missions: the monster and the kinds of attack as a number of bits")]
internal sealed partial class DailyMissionKillCommand : ICommand
{
    [ExternalDependency]
    private DailyMissionManager Manager { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var monster))
        {
            return false;
        }

        _ = uint.TryParse(tokens.ElementAtOrDefault(1), out var conditions);

        await Manager.OnMonsterKilledAsync(player, monster, conditions, CancellationToken.None);

        return true;
    }
}
