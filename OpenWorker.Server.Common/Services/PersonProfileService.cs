using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Persistence;

namespace OpenWorker.Server.Common.Services;

public sealed class PersonProfileService(
    World world,
    TitleBook titles,
    IDbContextFactory<PersistenceContext> factory,
    OtherPersonService others,
    FriendNotifier friends,
    IPlayerAudience audience) : PersonProfileServiceBase(world, titles, factory, others, friends)
{
    protected override void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
        audience.NearbyToOthers(player, message);
    }
}
