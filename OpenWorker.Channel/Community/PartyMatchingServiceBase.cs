using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Requests;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The auto matching: waiting for a party into a maze, leaving the wait, and the answer whether one goes.
/// </summary>
public abstract class PartyMatchingServiceBase(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyMatching matching,
    ReadOnlyCollection<MazeInfoRow> mazes
) :
    IHotspotHandler<PartyMatchingEnterRequest>,
    IHotspotHandler<PartyMatchingExitRequest>,
    IHotspotHandler<PartyMatchingCheckRequest>
{
    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingEnterRequest request)
    {
        var me = Me(context);

        if (request.Location is < short.MinValue or > short.MaxValue || !_mazes.TryGet((short)request.Location, out var maze))
        {
            Fail(context, PartyRules.Text.Unexpected);
            return;
        }

        if (await book.OfAsync(me) is not null)
        {
            Fail(context, PartyRules.Text.InParty);
            return;
        }

        if (await network.FindAsync(me) is { InMaze: true })
        {
            Fail(context, PartyRules.Text.InviterInMaze);
            return;
        }

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        if (await matching.EnterAsync(context.Player, maze.Id, PartyRules.MatchingLimit(maze.Admission)) is not 0 and var code)
        {
            Fail(context, code);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingExitRequest request)
    {
        var me = Me(context);

        // The client closes its window on the answer: one who waited nowhere hears it as well.
        if (!await matching.ExitAsync(me, PartyMatchingExit.None) && world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(new PartyMatchingExitResponse { Character = me, Reason = PartyMatchingExit.None });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingCheckRequest request)
    {
        await matching.CheckAsync(Me(context), request.IsCheck);
    }

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private void Fail(ServiceHandleContext context, int code)
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player)
                .Send(new ErrorMessageResponse(new MessageOpcode((byte)GroupOpcode.Party, (byte)PartyOpcode.MatchingEnter), (ushort)code));
        }
    }
}
