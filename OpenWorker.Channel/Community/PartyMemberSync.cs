using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.Persons;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Tells the others what changes about a person who stays on the server, so that what a party shows of a member is right: the
/// level the person reaches. (The change of channel tells it by itself, where the channel is changed.)
/// </summary>
public sealed partial class PartyMemberSync(ExperienceService experience, ILogger<PartyMemberSync> logger) : ICommunityModule
{
    void ICommunityModule.Attach(CommunityNetwork network)
    {
        experience.LevelReached += player => _ = TellAsync(network, player);
    }

    private async Task TellAsync(CommunityNetwork network, Entity player)
    {
        try
        {
            await network.ChangedAsync(player);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogNotTold(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The change of a person's level could not be told to the others.")]
    private static partial void LogNotTold(ILogger logger, Exception exception);
}
