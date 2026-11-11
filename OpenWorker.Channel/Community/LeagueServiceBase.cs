using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Requests;
using OpenWorker.Hotspot.Modules.League.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Leagues: founding, joining by invitation or application, the roles of the members, the notice and the board. A league is
/// in the database, so any server can change it; the members, wherever they are, are told through the network.
/// </summary>
public abstract class LeagueServiceBase(
    World world,
    CommunityNetwork network,
    IDbContextFactory<PersistenceContext> factory,
    LeagueBook book,
    LeagueNotifier notifier,
    LeagueSnapshots snapshots,
    IPersonRelations relations,
    LeagueLevels levels
) :
    IHotspotHandler<LeagueCreateRequest>,
    IHotspotHandler<LeagueDeleteRequest>,
    IHotspotHandler<LeagueListRequest>,
    IHotspotHandler<LeagueSearchRequest>,
    IHotspotHandler<LeagueOverlapNameRequest>,
    IHotspotHandler<LeagueInviteRequest>,
    IHotspotHandler<LeagueInviteAcceptRequest>,
    IHotspotHandler<LeagueInviteRejectRequest>,
    IHotspotHandler<LeagueWithdrawRequest>,
    IHotspotHandler<LeagueKickRequest>,
    IHotspotHandler<LeagueApplicantRequest>,
    IHotspotHandler<LeagueApplicantAcceptRequest>,
    IHotspotHandler<LeagueApplicantRejectRequest>,
    IHotspotHandler<LeagueBoardRequest>,
    IHotspotHandler<LeagueAuthChangeRequest>,
    IHotspotHandler<LeagueNoticeChangeRequest>,
    IHotspotHandler<LeaguePositionNameChangeRequest>,
    IHotspotHandler<LeagueMemberPositionChangeRequest>,
    IHotspotHandler<LeagueOpenOrNotRequest>,
    IHotspotHandler<LeagueRecruitNoticeRequest>
{
    /// <summary>
    /// The numbers of the texts the client shows, see its strings.
    /// </summary>
    public static class Text
    {
        public const int NameTaken = 57001;
        public const int NotInLeague = 57005;
        public const int NoRights = 57006;
        public const int NoSuchPerson = 57007;
        public const int InLeagueAlready = 57008;
        public const int InviteFailed = 57010;
        public const int CreateFailed = 57011;
        public const int LevelTooLow = 57012;
        public const int NotEnoughGold = 57013;
        public const int NotMaster = 57015;
        public const int NoSuchLeague = 57016;
        public const int ApplyFailed = 57017;
        public const int LeagueFull = 57018;
        public const int AcceptFailed = 57019;
        public const int ApplicantsFull = 57023;
        public const int JoinPenalty = 57025;
        public const int AlreadyApplied = 57026;
        public const int BadPositionName = 57027;
        public const int IsMaster = 57028;
        public const int PositionTaken = 57030;
        public const int NoticeTooLong = 57031;
        public const int SearchTooShort = 57032;
        public const int BadName = 57033;
        public const int KickYourself = 57034;
        public const int ForbiddenText = 57035;
        public const int ChangeYourself = 57036;
        public const int InviteeHasPenalty = 57037;
        public const int InviteeBusy = 57038;
        public const int RecruitNoticeTooLong = 57039;
        public const int Offline = 57000;
        public const int BlockedInviter = 57042;
        public const int Refused = 57043;
    }

    /// <summary>
    /// What a position may do in a new league, a slot for each position from the base one to the sub master and one for the
    /// master: the base members nothing, the named positions the care for new members, the elders, managers and chairmen that
    /// and the notice, the sub master everything. The master changes it in the window of the league.
    /// </summary>
    public static int[] DefaultAuth { get; } =
    [
        0,
        (int)LeagueRight.Join,
        (int)LeagueRight.Join,
        (int)LeagueRight.Join,
        (int)(LeagueRight.Join | LeagueRight.Notice),
        (int)(LeagueRight.Join | LeagueRight.Notice),
        (int)(LeagueRight.Join | LeagueRight.Notice),
        (int)LeagueRight.All,
        (int)LeagueRight.All
    ];

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueOverlapNameRequest request)
    {
        var name = request.Name.Trim();

        if (CheckName(name) is { } error)
        {
            Fail(context, LeagueOpcode.OverlapName, error);
            return;
        }

        var used = await factory.NameTakenAsync(name);

        if (Alive(context))
        {
            Session(context).Send(new LeagueOverlapNameResponse { Used = used });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueCreateRequest request)
    {
        var me = Me(context);
        var name = request.Name.Trim();

        if (CheckName(name) is { } error)
        {
            Fail(context, LeagueOpcode.Create, error);
            return;
        }

        if (await factory.MemberAsync(me) is not null)
        {
            Fail(context, LeagueOpcode.Create, Text.InLeagueAlready);
            return;
        }

        if (LevelOf(context) < LeagueRules.MinLevel)
        {
            Fail(context, LeagueOpcode.Create, Text.LevelTooLow);
            return;
        }

        if (world.Get<CurrencyComponent>(context.Player).Gold < LeagueRules.CreateCost)
        {
            Fail(context, LeagueOpcode.Create, Text.NotEnoughGold);
            return;
        }

        // The gold is taken before the league is written, so it cannot be spent meanwhile, and given back when no league is made.
        Charge(context.Player, -LeagueRules.CreateCost);

        var (outcome, remain, league) = await factory.CreateAsync(name, me, DefaultAuth);

        if (outcome != LeagueCreateOutcome.Created && Alive(context))
        {
            Charge(context.Player, LeagueRules.CreateCost);
        }

        if (!Alive(context))
        {
            return;
        }

        switch (outcome)
        {
            case LeagueCreateOutcome.Cooldown:
                Session(context).Send(new LeagueCreateResResponse { Remain = (long)remain.TotalSeconds });
                return;

            case LeagueCreateOutcome.NameTaken:
                Fail(context, LeagueOpcode.Create, Text.NameTaken);
                return;

            case LeagueCreateOutcome.InLeague:
                Fail(context, LeagueOpcode.Create, Text.InLeagueAlready);
                return;
        }

        var session = Session(context);

        var online = await network.FindAsync(me);
        var member = await factory.MemberAsync(me);

        if (member is not null)
        {
            session.Send(new LeagueCreateResponse
            {
                Info = LeagueMapper.ToInfo(league!),
                Member = LeagueMapper.ToMember(member, online)
            });
        }

        await notifier.SyncAsync([me], league);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueDeleteRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.Delete);

        if (mine is not var (member, league))
        {
            return;
        }

        if (member.Position != LeagueRules.Master)
        {
            Fail(context, LeagueOpcode.Delete, Text.NotMaster);
            return;
        }

        var people = await factory.MemberIdsAsync(league.Id);

        await factory.DisbandAsync(league.Id, Me(context));

        await network.SendAsync(people, new LeagueDeleteResponse { Result = 0 });
        await notifier.SyncAsync(people, null);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueListRequest request)
    {
        var leagues = await factory.ListAsync(request.Type == 1, LeagueRules.PageSize);

        if (Alive(context))
        {
            Session(context).Send(new LeagueListResponse { Leagues = leagues.Select(LeagueMapper.ToInfo).ToArray(), Actors = leagues.Select(e => e.MasterId).ToArray() });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueSearchRequest request)
    {
        var name = request.Name.Trim();
        var master = request.Master.Trim();

        if (name.Length < LeagueRules.MinSearchLength && master.Length < LeagueRules.MinSearchLength)
        {
            Fail(context, LeagueOpcode.Search, Text.SearchTooShort);
            return;
        }

        var leagues = await factory.SearchAsync(name, master, LeagueRules.PageSize);

        if (Alive(context))
        {
            Session(context).Send(new LeagueSearchResponse
            {
                State = request.State,
                Name = name,
                Master = master,
                Leagues = leagues.Select(LeagueMapper.ToInfo).ToArray(),
                Actors = leagues.Select(e => e.MasterId).ToArray()
            });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueInviteRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.Invite);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.Join))
        {
            Fail(context, LeagueOpcode.Invite, Text.NoRights);
            return;
        }

        var me = Me(context);
        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        if (await factory.FindPersonAsync(request.TargetName) is not { } target)
        {
            Fail(context, LeagueOpcode.Invite, Text.NoSuchPerson);
            return;
        }

        if (await network.FindAsync(target.Person) is null)
        {
            Fail(context, LeagueOpcode.Invite, Text.Offline);
            return;
        }

        if (await factory.MemberAsync(target.Person) is not null)
        {
            Fail(context, LeagueOpcode.Invite, Text.InLeagueAlready);
            return;
        }

        if (await factory.JoinCooldownAsync(target.Person) is not null)
        {
            Fail(context, LeagueOpcode.Invite, Text.InviteeHasPenalty);
            return;
        }

        if (league.MemberCount >= MaxMembers(league))
        {
            Fail(context, LeagueOpcode.Invite, Text.LeagueFull);
            return;
        }

        var invitation = new LeagueInviteResResponse
        {
            LeagueName = league.Name,
            TargetName = target.Name,
            RequesterName = myName,
            Actor = me,
            TargetActor = target.Person,
            League = league.Id,
            Result = 0
        };

        if (await relations.IsBlockedAsync(target.Person, me))
        {
            if (Alive(context))
            {
                Session(context).Send(invitation with { Result = Text.BlockedInviter });
            }

            return;
        }

        if (!await book.InviteAsync(target.Person, new LeagueInvite(league.Id, me, myName)))
        {
            Fail(context, LeagueOpcode.Invite, Text.InviteeBusy);
            return;
        }

        await network.SendAsync(target.Person, new LeagueInviteResponse
        {
            LeagueName = league.Name,
            TargetName = target.Name,
            RequesterName = myName,
            Actor = me,
            TargetActor = target.Person,
            League = league.Id,
            Result = 0
        });

        if (Alive(context))
        {
            Session(context).Send(invitation);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueInviteAcceptRequest request)
    {
        var me = Me(context);
        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        // The invitation is what the server remembers, not what the client says it is.
        if (await book.TakeInviteAsync(me) is not { } invite)
        {
            Fail(context, LeagueOpcode.InviteAccept, Text.InviteFailed);
            return;
        }

        var league = await factory.LeagueAsync(invite.League);

        if (league is null)
        {
            Fail(context, LeagueOpcode.InviteAccept, Text.NoSuchLeague);
            return;
        }

        var outcome = await factory.JoinAsync(league.Id, me, MaxMembers(league));

        if (!Alive(context))
        {
            return;
        }

        switch (outcome)
        {
            case LeagueJoinOutcome.Joined:
                await JoinedByAsync(league, me);

                await network.SendAsync(invite.Inviter, new LeagueInviteAcceptResponse
                {
                    Requester = invite.Inviter,
                    Target = me,
                    TargetName = myName,
                    League = league.Id,
                    Result = 0
                });
                break;

            case LeagueJoinOutcome.Full:
                Fail(context, LeagueOpcode.InviteAccept, Text.LeagueFull);
                break;

            case LeagueJoinOutcome.InLeague:
                Fail(context, LeagueOpcode.InviteAccept, Text.InLeagueAlready);
                break;

            case LeagueJoinOutcome.Penalty:
                Fail(context, LeagueOpcode.InviteAccept, Text.JoinPenalty);
                break;

            default:
                Fail(context, LeagueOpcode.InviteAccept, Text.NoSuchLeague);
                break;
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueInviteRejectRequest request)
    {
        var me = Me(context);
        var myName = world.Get<PersonInfoComponent>(context.Player).Name;

        if (await book.TakeInviteAsync(me) is not { } invite)
        {
            return;
        }

        await network.SendAsync(invite.Inviter, new LeagueInviteRejectResponse
        {
            TargetName = myName,
            Requester = invite.Inviter,
            Target = me,
            Result = Text.Refused
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueWithdrawRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.Withdraw);

        if (mine is not var (member, league))
        {
            return;
        }

        var me = Me(context);

        // A master leaves by disbanding.
        if (member.Position == LeagueRules.Master)
        {
            Fail(context, LeagueOpcode.Withdraw, Text.IsMaster);
            return;
        }

        if (!await factory.LeaveAsync(me, penalty: true))
        {
            return;
        }

        var others = (await factory.MemberIdsAsync(league.Id)).ToArray();

        await network.SendAsync(me, new LeagueWithdrawResResponse { Remain = (long)LeagueRules.WithdrawPenalty.TotalSeconds });
        await notifier.SyncAsync([me], null);

        await network.SendAsync(others, new LeagueWithdrawResponse { Actor = me });
        await InfoChangedAsync(league.Id, others);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueKickRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.Kick);

        if (mine is not var (member, league))
        {
            return;
        }

        var me = Me(context);

        if (request.Target == me)
        {
            Fail(context, LeagueOpcode.Kick, Text.KickYourself);
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.KickOut))
        {
            Fail(context, LeagueOpcode.Kick, Text.NoRights);
            return;
        }

        var target = await factory.MemberAsync(request.Target);

        if (target is null || target.League != league.Id)
        {
            Fail(context, LeagueOpcode.Kick, Text.NoSuchPerson);
            return;
        }

        // Nobody throws out somebody above them or alike.
        if (target.Position == LeagueRules.Master || (target.Position >= member.Position && member.Position != LeagueRules.Master))
        {
            Fail(context, LeagueOpcode.Kick, target.Position == LeagueRules.Master ? Text.IsMaster : Text.NoRights);
            return;
        }

        if (!await factory.LeaveAsync(target.Person, penalty: true, league.Id))
        {
            return;
        }

        var others = (await factory.MemberIdsAsync(league.Id)).ToArray();

        if (Alive(context))
        {
            Session(context).Send(new LeagueKickResResponse { Result = 0 });
        }

        await network.SendAsync(target.Person, new LeagueKickResponse());
        await notifier.SyncAsync([target.Person], null);

        await network.SendAsync(others, new LeagueMemberLeaveResponse { Target = target.Person, Kicker = me });
        await InfoChangedAsync(league.Id, others);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueApplicantRequest request)
    {
        var me = Me(context);
        var name = world.Get<PersonInfoComponent>(context.Player).Name;
        var applicant = new LeagueApplicant
        {
            League = request.Applicant.League,
            Actor = me,
            Name = name,
            Level = LevelOf(context),
            Date = LeagueMapper.ToUnix(DateTime.UtcNow),
            Class = (byte)world.Get<PersonInfoComponent>(context.Player).Hero
        };

        var outcome = await factory.ApplyAsync(applicant.League, me);

        if (!Alive(context))
        {
            return;
        }

        var result = outcome switch
        {
            LeagueApplyOutcome.Applied => 0,
            LeagueApplyOutcome.NoLeague => Text.NoSuchLeague,
            LeagueApplyOutcome.Closed => Text.ApplyFailed,
            LeagueApplyOutcome.InLeague => Text.InLeagueAlready,
            LeagueApplyOutcome.AlreadyApplied => Text.AlreadyApplied,
            LeagueApplyOutcome.ListFull => Text.ApplicantsFull,
            _ => Text.JoinPenalty
        };

        var remain = result == Text.JoinPenalty ? (long)(await factory.JoinCooldownAsync(me) ?? TimeSpan.Zero).TotalSeconds : 0;

        if (!Alive(context))
        {
            return;
        }

        Session(context).Send(new LeagueApplicantResResponse { Applicant = applicant with { Result = result }, Remain = remain });

        if (outcome != LeagueApplyOutcome.Applied)
        {
            return;
        }

        // Those who may answer see the request.
        var league = await factory.LeagueAsync(applicant.League);
        var members = await factory.MembersAsync(applicant.League);

        if (league is not null)
        {
            await network.SendAsync(
                members.Where(e => league.Allows(e.Position, LeagueRight.Join)).Select(e => e.Person),
                new LeagueApplicantAddResponse { Applicant = applicant });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueApplicantAcceptRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.ApplicantAccept);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.Join))
        {
            Fail(context, LeagueOpcode.ApplicantAccept, Text.NoRights);
            return;
        }

        if (await factory.ApplicantAsync(league.Id, request.Actor) is null)
        {
            Fail(context, LeagueOpcode.ApplicantAccept, Text.AcceptFailed);
            return;
        }

        var outcome = await factory.JoinAsync(league.Id, request.Actor, MaxMembers(league));
        var members = (await factory.MembersAsync(league.Id)).Select(e => e.Person).ToArray();
        var officers = await OfficersAsync(league);

        if (outcome != LeagueJoinOutcome.Joined)
        {
            // Nothing will come of it: the one who applied is in another league or the room is gone.
            if (outcome != LeagueJoinOutcome.Full)
            {
                await factory.RemoveApplicantAsync(league.Id, request.Actor);
                await network.SendAsync(officers, new LeagueApplicantDeleteResponse { Actor = request.Actor });
            }

            Fail(context, LeagueOpcode.ApplicantAccept, outcome == LeagueJoinOutcome.Full ? Text.LeagueFull : Text.AcceptFailed);
            return;
        }

        await network.SendAsync(officers, new LeagueApplicantDeleteResponse { Actor = request.Actor });
        await JoinedByAsync(league, request.Actor);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueApplicantRejectRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.ApplicantReject);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.Join))
        {
            Fail(context, LeagueOpcode.ApplicantReject, Text.NoRights);
            return;
        }

        if (!await factory.RemoveApplicantAsync(league.Id, request.Target))
        {
            Fail(context, LeagueOpcode.ApplicantReject, Text.AcceptFailed);
            return;
        }

        var me = Me(context);

        await network.SendAsync(await OfficersAsync(league), new LeagueApplicantDeleteResponse { Actor = request.Target });

        if (Alive(context))
        {
            Session(context).Send(new LeagueApplicantRejectResResponse { League = league.Id, Target = request.Target, Actor = me, Result = 0 });
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueBoardRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.Board);

        if (mine is not var (member, league))
        {
            return;
        }

        var message = request.Entry.Message.Trim();

        if (message.Length == 0 || message.Length > LeagueRules.MaxBoardLength)
        {
            return;
        }

        var name = world.Get<PersonInfoComponent>(context.Player).Name;
        var note = LeagueMapper.ToBoard(await factory.AddBoardAsync(league.Id, member.Person, name, message));

        if (Alive(context))
        {
            Session(context).Send(new LeagueBoardResResponse { Entry = note, Remain = 0 });
        }

        await network.SendAsync((await factory.MemberIdsAsync(league.Id)).Where(e => e != member.Person), new LeagueBoardResponse { Entry = note });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueAuthChangeRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.AuthChange);

        if (mine is not var (member, league))
        {
            return;
        }

        var rewards = request.Rewards
            .Select(e => new LeagueRewardInfo { Auth = e.Auth & (int)LeagueRight.All, LimitGoldOut = Math.Max(0, e.LimitGoldOut) })
            .ToArray();

        if (!league.MayChange(member.Position, rewards.Select(e => e.Auth).ToArray(), rewards.Select(e => e.LimitGoldOut).ToArray()))
        {
            Fail(context, LeagueOpcode.AuthChange, Text.NoRights);
            return;
        }

        await factory.SetAuthAsync(league.Id, rewards.Select(e => e.Auth).ToArray(), rewards.Select(e => e.LimitGoldOut).ToArray());

        if (Alive(context))
        {
            Session(context).Send(new LeagueAuthChangeResResponse { Rewards = rewards, Result = 0 });
        }

        await network.SendAsync((await factory.MemberIdsAsync(league.Id)).Where(e => e != member.Person), new LeagueAuthChangeResponse { Rewards = rewards, Result = 0 });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueNoticeChangeRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.NoticeChange);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.Notice))
        {
            Fail(context, LeagueOpcode.NoticeChange, Text.NoRights);
            return;
        }

        var notice = request.Notice.Trim();

        if (notice.Length > LeagueRules.MaxNoticeLength)
        {
            Fail(context, LeagueOpcode.NoticeChange, Text.NoticeTooLong);
            return;
        }

        await factory.SetNoticeAsync(league.Id, notice);

        var date = LeagueMapper.ToUnix(DateTime.UtcNow);

        if (Alive(context))
        {
            Session(context).Send(new LeagueNoticeChangeResResponse { League = league.Id, Notice = notice, Date = date, Result = 0 });
        }

        await network.SendAsync(
            (await factory.MemberIdsAsync(league.Id)).Where(e => e != member.Person),
            new LeagueNoticeChangeResponse { League = league.Id, Notice = notice, Date = date, Result = 0 });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeaguePositionNameChangeRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.PositionNameChange);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.SetPosition))
        {
            Fail(context, LeagueOpcode.PositionNameChange, Text.NoRights);
            return;
        }

        var name = request.Name.Trim();

        if (request.Position is < 1 or > LeaguePersistent.PositionNameCount || name.Length is 0 or > LeagueRules.MaxPositionNameLength)
        {
            Fail(context, LeagueOpcode.PositionNameChange, Text.BadPositionName);
            return;
        }

        var previous = league.PositionNames[request.Position - 1];

        await factory.SetPositionNameAsync(league.Id, request.Position - 1, name);

        var message = new LeaguePositionNameChangeResponse { Position = request.Position, Name = name, Result = 0, PreviousName = previous };

        if (Alive(context))
        {
            Session(context).Send(new LeaguePositionNameChangeResResponse { Position = request.Position, Name = name, Result = 0, PreviousName = previous });
        }

        await network.SendAsync((await factory.MemberIdsAsync(league.Id)).Where(e => e != member.Person), message);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueMemberPositionChangeRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.MemberPositionChange);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.SetPosition))
        {
            Fail(context, LeagueOpcode.MemberPositionChange, Text.NoRights);
            return;
        }

        if (request.Actor == member.Person)
        {
            Fail(context, LeagueOpcode.MemberPositionChange, Text.ChangeYourself);
            return;
        }

        var target = await factory.MemberAsync(request.Actor);

        if (target is null || target.League != league.Id)
        {
            Fail(context, LeagueOpcode.MemberPositionChange, Text.NoSuchPerson);
            return;
        }

        // The client gives a position with a state of zero and takes it away by giving the base position.
        var position = request.Position;

        if (!LeagueRules.MayAppoint(member.Position, target.Position, position))
        {
            Fail(context, LeagueOpcode.MemberPositionChange, Text.NoRights);
            return;
        }

        var unnamed = position is >= LeagueRules.FirstNamed and <= LeagueRules.LastNamed &&
                      string.IsNullOrEmpty(league.PositionNames[position - LeagueRules.FirstNamed]);

        if (!LeagueRules.CanBeGiven(position) || unnamed || !await factory.SetPositionAsync(league.Id, target.Person, position))
        {
            Fail(context, LeagueOpcode.MemberPositionChange, Text.PositionTaken);
            return;
        }

        await network.SendAsync(
            await factory.MemberIdsAsync(league.Id),
            new LeagueMemberPositionChangeResponse { Actor = target.Person, Position = position, State = request.State, Result = 0 });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueOpenOrNotRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.OpenOrNot);

        if (mine is not var (member, league))
        {
            return;
        }

        // The client offers the switch to the master alone.
        if (member.Position != LeagueRules.Master)
        {
            Fail(context, LeagueOpcode.OpenOrNot, Text.NotMaster);
            return;
        }

        await factory.SetOpenAsync(league.Id, request.Open);
        await network.SendAsync(await factory.MemberIdsAsync(league.Id), new LeagueOpenOrNotResponse { League = league.Id, Open = request.Open });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, LeagueRecruitNoticeRequest request)
    {
        var mine = await MineAsync(context, LeagueOpcode.RecruitNotice);

        if (mine is not var (member, league))
        {
            return;
        }

        if (!league.Allows(member.Position, LeagueRight.RecruitNotice))
        {
            Fail(context, LeagueOpcode.RecruitNotice, Text.NoRights);
            return;
        }

        var notice = request.Notice.Trim();

        if (notice.Length > LeagueRules.MaxRecruitNoticeLength)
        {
            Fail(context, LeagueOpcode.RecruitNotice, Text.RecruitNoticeTooLong);
            return;
        }

        await factory.SetRecruitNoticeAsync(league.Id, notice);

        await network.SendAsync(
            await factory.MemberIdsAsync(league.Id),
            new LeagueRecruitNoticeResponse { League = league.Id, Notice = notice, Date = LeagueMapper.ToUnix(DateTime.UtcNow), Result = 0 });
    }

    /// <summary>
    /// The person is in the league now: they see all of it, and the others see them.
    /// </summary>
    private async ValueTask JoinedByAsync(LeagueRecord league, int person)
    {
        var fresh = await factory.LeagueAsync(league.Id);
        var member = await factory.MemberAsync(person);

        if (fresh is null || member is null)
        {
            return;
        }

        var online = await network.FindAsync(person);
        var others = (await factory.MemberIdsAsync(league.Id)).Where(e => e != person).ToArray();

        // The newcomer first: the others have to be told about somebody the newcomer's client already knows.
        await notifier.SyncAsync([person], fresh);

        if (await snapshots.InfoAsync(person, joined: true) is { } info)
        {
            await network.SendAsync(person, info);
        }

        await network.SendAsync(others, new LeagueJoinUserResponse { Member = LeagueMapper.ToMember(member, online) });
        await InfoChangedAsync(league.Id, others);
    }

    private async ValueTask InfoChangedAsync(int league, IEnumerable<int> members)
    {
        if (await factory.LeagueAsync(league) is not { } fresh)
        {
            return;
        }

        await network.SendAsync(members, LeagueMapper.ToUpdate(fresh));
    }

    private async Task<IEnumerable<int>> OfficersAsync(LeagueRecord league)
    {
        return (await factory.MembersAsync(league.Id)).Where(e => league.Allows(e.Position, LeagueRight.Join)).Select(e => e.Person).ToArray();
    }

    /// <summary>
    /// The member and the league of the player, or an error back to them if they are in none.
    /// </summary>
    private async Task<(LeagueMemberRecord Member, LeagueRecord League)?> MineAsync(ServiceHandleContext context, LeagueOpcode command)
    {
        var member = await factory.MemberAsync(Me(context));
        var league = member is null ? null : await factory.LeagueAsync(member.League);

        if (member is null || league is null)
        {
            Fail(context, command, Text.NotInLeague);
            return null;
        }

        return Alive(context) ? (member, league) : null;
    }

    /// <summary>
    /// Adds gold to the purse of the player, or takes it when the amount is negative, and shows the new purse.
    /// </summary>
    private void Charge(Entity player, long amount)
    {
        var currency = world.Get<CurrencyComponent>(player);

        currency.Gold += amount;
        world.MarkDirty(player, PersonSections.Currencies);

        world.Get<ServerSessionComponent>(player).Send(new ItemUpdateInvenMoneyResponse { Total = currency.Gold, Bonus = 0, Type = MoneyEarnFlow.Normal });
    }

    private int MaxMembers(LeagueRecord league) => levels.MemberLimit(league.Rating);

    private static int? CheckName(string name)
    {
        if (name.Length is < LeagueRules.MinNameLength or > LeagueRules.MaxNameLength)
        {
            return Text.BadName;
        }

        return name.All(char.IsLetterOrDigit) ? null : Text.ForbiddenText;
    }

    /// <summary>
    /// The level of the person who asks. Every rule that depends on the level asks here.
    /// </summary>
    private byte LevelOf(ServiceHandleContext context)
    {
        return world.TryGet<LevelComponent>(context.Player, out var level) ? level.Level : PersonModuleDefines.DefaultLevel;
    }

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private bool Alive(ServiceHandleContext context) => world.IsAlive(context.Player);

    private ServerSessionComponent Session(ServiceHandleContext context) => world.Get<ServerSessionComponent>(context.Player);

    private void Fail(ServiceHandleContext context, LeagueOpcode command, int code)
    {
        if (Alive(context))
        {
            Session(context).Send(new ErrorMessageResponse(new MessageOpcode((byte)GroupOpcode.League, (byte)command), (ushort)code));
        }
    }
}
