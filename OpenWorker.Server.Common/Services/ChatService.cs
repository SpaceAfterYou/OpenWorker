using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Commands;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Server.Common.Services;

public sealed class ChatService(
    World world,
    CommandManager commands,
    IPlayerAudience audience,
    CommunityNetwork network,
    ChatThrottle throttle,
    IPartyRoster parties,
    ILeagueRoster leagues,
    IPersonRelations relations,
    IMegaphoneSource megaphones
) :
    ChatServiceBase(world, network, throttle, parties, leagues, relations, megaphones)
{
    protected override void SendNearby(Entity player, IResponseHotspotMessage message)
    {
        audience.Chat(player, message);
    }

    protected override ValueTask<bool> TryCommandAsync(Entity player, string text)
    {
        return commands.TryExecute(player, text);
    }
}
