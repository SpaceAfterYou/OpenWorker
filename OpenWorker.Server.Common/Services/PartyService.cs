using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Server.Common.Services;

public sealed class PartyService(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier notifier,
    IPersonRelations relations
) :
    PartyServiceBase(world, network, book, notifier, relations);

public sealed class PartyRecruitService(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier parties,
    PartyRecruitBoard board,
    PartyAwaiterBoard awaiters,
    PartyRecruitNotifier recruits,
    OtherPersonService others,
    TimeProvider time
) :
    PartyRecruitServiceBase(world, network, book, parties, board, awaiters, recruits, others, time);

public sealed class PartyMatchingService(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyMatching matching,
    ReadOnlyCollection<MazeInfoRow> mazes,
    MazeAdmission? admission = null
) :
    PartyMatchingServiceBase(world, network, book, matching, mazes, admission);
