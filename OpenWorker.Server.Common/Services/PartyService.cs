using Arch.Core;
using OpenWorker.Channel.Community;

namespace OpenWorker.Server.Common.Services;

public sealed class PartyService(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier notifier,
    IPersonRelations relations
) :
    PartyServiceBase(world, network, book, notifier, relations);
