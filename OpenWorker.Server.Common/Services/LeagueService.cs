using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel.Community;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Server.Common.Services;

public sealed class LeagueService(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    LeagueBook book,
    LeagueNotifier notifier,
    LeagueSnapshots snapshots,
    IPersonRelations relations,
    ReadOnlyCollection<LeagueInfoRow> levels
) :
    LeagueServiceBase(world, network, factory, book, notifier, snapshots, relations, levels);
