using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Commands;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.MazeServer.Services;

public sealed class ChatService(
    World world,
    CommandManager commands,
    CommunityNetwork network,
    ChatThrottle throttle,
    IPartyRoster parties,
    ILeagueRoster leagues,
    IPersonRelations relations,
    IMegaphoneSource megaphones
) :
    ChatServiceBase(world, network, throttle, parties, leagues, relations, megaphones)
{
    private readonly World _world = world;

    /// <summary>
    /// Every player has a maze of their own, so there is nobody close by to hear but the player.
    /// </summary>
    protected override void SendNearby(Entity player, IResponseHotspotMessage message)
    {
        _world.Get<ServerSessionComponent>(player).Send(message);
    }

    protected override ValueTask<bool> TryCommandAsync(Entity player, string text)
    {
        return commands.TryExecute(player, text);
    }
}
