using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Persistence;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class FriendService(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    FriendRecruitBoard recruits,
    OtherPersonService others
) :
    FriendServiceBase(world, network, factory, recruits, others);
