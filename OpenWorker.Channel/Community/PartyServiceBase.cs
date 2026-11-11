using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Requests;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The party itself: inviting, answering an invitation, the master, kicking out, leaving and the list. The district and the
/// maze both run it, so a member in a maze can leave or be kicked too.
/// </summary>
public abstract class PartyServiceBase(
    World world,
    CommunityNetwork network,
    PartyBook book,
    PartyNotifier notifier,
    IPersonRelations relations
) :
    IHotspotHandler<PartyInviteRequest>,
    IHotspotHandler<PartyAcceptRequest>,
    IHotspotHandler<PartyCancelRequest>,
    IHotspotHandler<PartyChangeMasterRequest>,
    IHotspotHandler<PartyKickOutRequest>,
    IHotspotHandler<PartyLeaveRequest>,
    IHotspotHandler<PartyUpdateInfoRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyInviteRequest request)
    {
        var me = Me(context);
        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        if (await network.FindByNameAsync(request.TargetName) is not { } target)
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.NoMember);
            return;
        }

        if (target.Person == me)
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.Yourself);
            return;
        }

        if (await network.FindAsync(me) is { InMaze: true })
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.InviterInMaze);
            return;
        }

        if (target.InMaze)
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.InvitedInMaze);
            return;
        }

        if (await book.OfAsync(me) is { } mine)
        {
            if (mine.Master != me)
            {
                Fail(context, PartyOpcode.Invite, PartyRules.Text.NotMaster);
                return;
            }

            if (mine.IsFull)
            {
                Fail(context, PartyOpcode.Invite, PartyRules.Text.Full);
                return;
            }
        }

        if (await book.OfAsync(target.Person) is not null)
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.InParty);
            return;
        }

        if (await relations.IsBlockedAsync(me, target.Person))
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.YouBlocked);
            return;
        }

        if (await relations.IsBlockedAsync(target.Person, me))
        {
            Fail(context, PartyOpcode.Invite, PartyRules.Text.CannotParty);
            return;
        }

        if (await book.InviteAsync(target.Person, new PartyInvite(me, myName)) is { } waiting)
        {
            Fail(context, PartyOpcode.Invite, waiting.Inviter == me ? PartyRules.Text.AlreadyInviting : PartyRules.Text.OtherInvitation);
            return;
        }

        await network.SendAsync(target.Person, new PartyInviteResponse
        {
            TargetName = target.Name,
            RequesterName = myName,
            RequesterActor = new ActorValue(me, ActorType.User),
            TargetActor = new ActorValue(target.Person, ActorType.User),
            Server = 0,
            Result = 0
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyAcceptRequest request)
    {
        var me = Me(context);

        // The invitation is what the server remembers, not what the client says it is.
        if (await book.TakeInviteAsync(me) is not { } invite)
        {
            Refuse(context, PartyRules.Text.CannotJoin);
            return;
        }

        var online = await network.FindManyAsync([invite.Inviter, me]);

        if (online[0] is not { } master)
        {
            Refuse(context, PartyRules.Text.NoMember);
            return;
        }

        if (master.InMaze || online[1] is { InMaze: true } || !Alive(context))
        {
            Refuse(context, PartyRules.Text.CannotJoin);
            return;
        }

        var outcome = await notifier.JoinAsync(notifier.CardOf(master), notifier.CardOf(context.Player));

        switch (outcome)
        {
            case PartyJoinOutcome.Full:
                Refuse(context, PartyRules.Text.Full);
                break;

            case PartyJoinOutcome.InParty or PartyJoinOutcome.NotMaster:
                Refuse(context, PartyRules.Text.CannotJoin);
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyCancelRequest request)
    {
        var me = Me(context);
        // Only the one who invited hears it, and only of an invitation the server remembers: the client cannot name
        // somebody else to be told. The client says why it declined by itself: busy, or nothing.
        if (await book.TakeInviteAsync(me) is not { Inviter: not 0 and var inviter } || !Alive(context))
        {
            return;
        }

        await network.SendAsync(inviter, new PartyCancelResponse
        {
            ReqActor = new ActorValue(inviter, ActorType.User),
            RejActor = new ActorValue(me, ActorType.User),
            RejName = world.Get<PersonInfoComponent>(context.Player).Name,
            ErrorCode = request.ErrorCode == PartyRules.Text.Busy ? PartyRules.Text.Busy : 0
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyChangeMasterRequest request)
    {
        var me = Me(context);
        var next = request.Master.Identifier;

        if (await book.OfAsync(me) is not { } party)
        {
            Fail(context, PartyOpcode.ChangeMaster, PartyRules.Text.NotInParty);
            return;
        }

        if (party.Master != me)
        {
            Fail(context, PartyOpcode.ChangeMaster, PartyRules.Text.NotMaster);
            return;
        }

        if (next == me)
        {
            Fail(context, PartyOpcode.ChangeMaster, PartyRules.Text.Yourself);
            return;
        }

        if (!party.Has(next))
        {
            Fail(context, PartyOpcode.ChangeMaster, PartyRules.Text.NoMember);
            return;
        }

        if (!await notifier.HandOverAsync(party, next))
        {
            Fail(context, PartyOpcode.ChangeMaster, PartyRules.Text.ChangeMasterFailed);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyKickOutRequest request)
    {
        var me = Me(context);
        var target = request.Member.Identifier;

        if (await book.OfAsync(me) is not { } party)
        {
            Fail(context, PartyOpcode.KickOut, PartyRules.Text.NotInParty);
            return;
        }

        if (target == me)
        {
            Fail(context, PartyOpcode.KickOut, PartyRules.Text.Yourself);
            return;
        }

        if (party.Master != me)
        {
            Fail(context, PartyOpcode.KickOut, PartyRules.Text.NotMaster);
            return;
        }

        if (!party.Has(target) || !await notifier.LeaveAsync(target, kicked: true))
        {
            Fail(context, PartyOpcode.KickOut, PartyRules.Text.NoMember);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyLeaveRequest request)
    {
        if (!await notifier.LeaveAsync(Me(context), kicked: false))
        {
            Fail(context, PartyOpcode.Leave, PartyRules.Text.NotInParty);
        }
    }

    /// <summary>
    /// The client asks for its party every time it enters a district or a maze. Somebody alone gets no answer: the
    /// client shows every error answer in a window, so an error here would pop up at each entry.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, PartyUpdateInfoRequest request)
    {
        if (await book.OfAsync(Me(context)) is not { } party)
        {
            return;
        }

        var list = await notifier.ListAsync(party, PartyUpdateType.None);

        if (Alive(context))
        {
            Session(context).Send(list);
        }
    }

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private bool Alive(ServiceHandleContext context) => world.IsAlive(context.Player);

    private ServerSessionComponent Session(ServiceHandleContext context) => world.Get<ServerSessionComponent>(context.Player);

    /// <summary>
    /// An answer to an invitation that did not lead into the party; the client shows that it could not join.
    /// </summary>
    private void Refuse(ServiceHandleContext context, int code)
    {
        if (Alive(context))
        {
            Session(context).Send(new PartyAcceptResponse { Identifier = Me(context), Result = code });
        }
    }

    private void Fail(ServiceHandleContext context, PartyOpcode command, int code)
    {
        if (Alive(context))
        {
            Session(context).Send(new ErrorMessageResponse(new MessageOpcode((byte)GroupOpcode.Party, (byte)command), (ushort)code));
        }
    }
}
