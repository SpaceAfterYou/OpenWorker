using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Commands;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.DistrictServer.Services;

public sealed class ChatService(
    World world,
    CommandManager commands,
    ServiceChannels channels,
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
        channels.Get(player).Broadcast(message);
    }

    protected override ValueTask<bool> TryCommandAsync(Entity player, string text)
    {
        return commands.TryExecute(player, text);
    }
}
