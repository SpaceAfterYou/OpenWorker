using Arch.Core;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Hotspot.Modules.Boosters.Enums;

namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Loads what the features of this group keep for a person when the person enters a server that is not the district:
/// each of them reads its state and tells the client. The district does it step by step while the person enters.
/// </summary>
public sealed class MiscStateLoader(
    SoulMetryManager soulMetry,
    DailyMissionManager dailyMissions,
    BoosterManager boosters,
    HelperManager helpers,
    ItemUseLimits itemUse)
{
    public async ValueTask EnterMazeAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        await itemUse.EnterAsync(player, person, cancellationToken);
        await soulMetry.EnterAsync(player, person, cancellationToken);
        await dailyMissions.EnterAsync(player, person, cancellationToken);
        await helpers.EnterMazeAsync(player, person, cancellationToken);
        await boosters.EnterAsync(player, person, BoosterConsumeArea.Maze, cancellationToken);
    }
}
