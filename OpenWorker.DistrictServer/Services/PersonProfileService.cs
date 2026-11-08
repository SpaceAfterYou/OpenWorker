using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Persistence;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class PersonProfileService(
    World world,
    TitleBook titles,
    IDbContextFactory<PersistenceContext> factory,
    OtherPersonService others,
    FriendNotifier friends,
    ServiceChannels channels) : PersonProfileServiceBase(world, titles, factory, others, friends)
{
    protected override void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        channels.Get(player).BroadcastToOthers(player, message);
    }
}
