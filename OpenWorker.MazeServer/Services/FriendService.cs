using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Persistence;

namespace OpenWorker.MazeServer.Services;

public sealed class FriendService(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    FriendRecruitBoard recruits,
    OtherPersonService others
) :
    FriendServiceBase(world, network, factory, recruits, others);
