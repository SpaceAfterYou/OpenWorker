using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Gameplay.Modules.Ranking;

namespace OpenWorker.Commands;

/// <summary>
/// Ends the period of the time attack ranking now, as the end of the week does: the places get their rewards by letter and
/// the lists start empty. <c>/ranking end</c>.
/// </summary>
[CommandTrigger("ranking", "end")]
[CommandDescription("End the period of the time attack ranking now: rewards by letter, empty lists")]
internal sealed partial class RankingEndCommand : ICommand
{
    [ExternalDependency]
    private TimeAttackPeriods Periods { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        return await Periods.EndNowAsync() is not null;
    }
}
