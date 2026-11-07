using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Persistence;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Every player has a maze of their own for now, so there are no others to tell.
/// </summary>
[HotspotHandler(HotspotHandlerType.District)]
public sealed class PersonProfileService(
    World world,
    TitleBook titles,
    IDbContextFactory<PersistenceContext> factory,
    OtherPersonService others,
    FriendNotifier friends) : PersonProfileServiceBase(world, titles, factory, others, friends)
{
    protected override void BroadcastToOthers(Entity player, IResponseHotspotMessage message)
    {
    }
}
